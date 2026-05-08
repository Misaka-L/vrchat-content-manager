using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Threading;
using Downloader;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateService(
    IUpdateInstallationService updateInstallationService,
    IHttpClientFactory httpClientFactory,
    ILogger<AppUpdateService> logger,
    AppLifetimeService lifetimeService
)
{
    public bool IsAppUpdateSupported() => updateInstallationService.IsUpdateInstallationSupported();

    public AppUpdateInformation? UpdateInformation { get; private set; }
    public AppUpdateServiceState UpdateState { get; private set; } = AppUpdateServiceState.Idle;
    public event EventHandler<AppUpdateServiceState>? OnUpdateStateChanged;

    private string? _pathToDownloadFile;

    #region Download

    private IDownload? _downloadTask;

    public Exception? LastException { get; private set; }
    public double? BytesPerSecondSpeed { get; private set; }
    public long? TotalFileSize { get; private set; }
    public long? DownloadedFileSize { get; private set; }

    private CancellationTokenSource? _downloadCts;

    public void StartDownloadUpdate(AppUpdateInformation updateInformation)
    {
        if (UpdateState != AppUpdateServiceState.Idle)
            throw new InvalidOperationException("Update Service are not in Idle state");

        if (!IsAppUpdateSupported())
            throw new NotSupportedException("Update are not supported for this platform");

        if (!updateInformation.Platforms
                .TryGetValue(updateInstallationService.GetPlatformIdentifier(), out var platformInformation))
        {
            throw new ArgumentException(
                $"Platform {updateInstallationService.GetPlatformIdentifier()} is not supported for this update");
        }

        logger.LogInformation("Starting download update {Version} Sha256: {Sha256} Url: {DownloadUrl}",
            updateInformation.Version, platformInformation.Sha256, platformInformation.Url);

        UpdateInformation = updateInformation;
        OnOnUpdateStateChanged(AppUpdateServiceState.Downloading);

        _downloadCts = new CancellationTokenSource();
        var cancellationToken = _downloadCts.Token;

        _pathToDownloadFile = Path.Combine(AppStorageService.GetTempPath(), "update", "package");

        var download = DownloadBuilder.New()
            .WithUrl(platformInformation.Url)
            .WithFileLocation(_pathToDownloadFile)
            .WithConfiguration(new DownloadConfiguration
            {
                ParallelCount = 8,
                ParallelDownload = true,
                EnableAutoResumeDownload = false
            })
            .WithHttpClient(() => httpClientFactory.CreateClient(nameof(AppUpdateService)))
            .Build();

        download.DownloadProgressChanged += (_, args) =>
        {
            BytesPerSecondSpeed = args.BytesPerSecondSpeed;
            TotalFileSize = download.TotalFileSize;
            DownloadedFileSize = download.DownloadedFileSize;
        };

        _downloadTask = download;
        _ = Task.Run(async () =>
        {
            try
            {
                await download.StartAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                await using var fileStream = File.OpenRead(_pathToDownloadFile);
                var fileSha256 = await ComputeSha256Async(fileStream, cancellationToken);
                var remoteSha256 = platformInformation.Sha256;

                if (!string.Equals(fileSha256, remoteSha256, StringComparison.OrdinalIgnoreCase))
                    throw new UpdateFileIntegrityCheckFailedException(fileSha256, remoteSha256);
            }
            catch (UpdateFileIntegrityCheckFailedException ex)
            {
                logger.LogError(
                    "Downloaded file sha256 didn't match remote sha256, Remote: {RemoteSha256} Local: {FileSha256}",
                    ex.RemoteSha256, ex.LocalSha256
                );

                OnOnUpdateStateChanged(AppUpdateServiceState.IntegrityCheckFailed);
                NotifException(ex);

                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to download update");
                OnOnUpdateStateChanged(AppUpdateServiceState.DownloadError);
                NotifException(ex);

                return;
            }
            finally
            {
                await _downloadTask.DisposeAsync();
                _downloadTask = null;
            }

            logger.LogInformation("Update downloaded and waiting for install");
            OnOnUpdateStateChanged(AppUpdateServiceState.WaitingForInstall);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<string> ComputeSha256Async(
        Stream stream, CancellationToken cancellationToken = default
    )
    {
        using var sha256 = SHA256.Create();
        var fileHashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        var fileHash = Convert.ToHexStringLower(fileHashBytes);

        return fileHash;
    }

    private void NotifException(Exception ex)
    {
        LastException = ex;
    }

    #endregion

    public async ValueTask InstallUpdateAsync()
    {
        if (UpdateState != AppUpdateServiceState.WaitingForInstall)
            throw new InvalidOperationException("Update Service not in WaitingForInstall state");

        try
        {
            if (_pathToDownloadFile is null)
            {
                Debug.Fail("_pathToDownloadFile should not be null when WaitingForInstall");
                throw new InvalidOperationException("_pathToDownloadFile should not be null when WaitingForInstall");
            }

            await updateInstallationService.InstallUpdateAsync(_pathToDownloadFile);
            Dispatcher.UIThread.Invoke(lifetimeService.Shutdown);
        }
        catch (Exception ex)
        {
            NotifException(ex);
            OnOnUpdateStateChanged(AppUpdateServiceState.InstallError);
            throw;
        }
    }

    public async ValueTask RetryUpdateAsync()
    {
        if (UpdateState is not (AppUpdateServiceState.DownloadError or AppUpdateServiceState.IntegrityCheckFailed))
            throw new InvalidOperationException("Update Service not in any error state");

        if (UpdateInformation is null)
        {
            Debug.Fail("UpdateInformation should not be null in error state");
            throw new InvalidOperationException("UpdateInformation should not be null in error state");
        }

        var update = UpdateInformation;
        await CancelUpdateAsync();
        StartDownloadUpdate(update);
    }

    public async ValueTask CancelUpdateAsync()
    {
        logger.LogInformation("Canceling update");

        if (_downloadCts is not null)
            await _downloadCts.CancelAsync();
        _downloadCts = null;
        _downloadTask = null;

        if (_pathToDownloadFile is not null && File.Exists(_pathToDownloadFile))
        {
            try
            {
                File.Delete(_pathToDownloadFile);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cleanup downloaded file");
            }
        }

        _downloadTask = null;
        UpdateInformation = null;
        BytesPerSecondSpeed = null;
        DownloadedFileSize = null;
        TotalFileSize = null;
        _pathToDownloadFile = null;

        OnOnUpdateStateChanged(AppUpdateServiceState.Idle);
    }

    private void OnOnUpdateStateChanged(AppUpdateServiceState e)
    {
        UpdateState = e;
        OnUpdateStateChanged?.Invoke(this, e);
    }
}

public enum AppUpdateServiceState
{
    Idle,
    Downloading,
    DownloadError,
    IntegrityCheckFailed,
    WaitingForInstall,
    InstallError
}

public sealed class UpdateFileIntegrityCheckFailedException(string localSha256, string remoteSha256)
    : Exception(
        $"Download file has different sha256 compare to remote metadata, local: {localSha256}, remote: {remoteSha256}"
    )
{
    public string LocalSha256 => localSha256;
    public string RemoteSha256 => remoteSha256;
}