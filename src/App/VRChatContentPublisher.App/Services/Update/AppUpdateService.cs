using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Threading;
using Downloader;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.Core.Shared;
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

    /// <summary>
    /// The update the service is currently working on. Never null while <see cref="UpdateState"/> is not
    /// <see cref="AppUpdateServiceState.Idle"/>: the error states have to be retryable.
    /// </summary>
    public AppUpdateInformation? UpdateInformation { get; private set; }

    public AppUpdateServiceState UpdateState { get; private set; } = AppUpdateServiceState.Idle;
    public event EventHandler<AppUpdateServiceState>? OnUpdateStateChanged;

    private string? _pathToDownloadFile;

    #region Download

    public Exception? LastException { get; private set; }
    public double? BytesPerSecondSpeed { get; private set; }
    public long? TotalFileSize { get; private set; }
    public long? DownloadedFileSize { get; private set; }

    /// <summary>
    /// Guards <see cref="UpdateState"/>, <see cref="UpdateInformation"/> and <see cref="_operationGeneration"/>.
    /// Every download operation owns a generation, and only the current generation is allowed
    /// to change the state: <see cref="CancelUpdateAsync"/> bumps the generation before cancelling, so all
    /// late state changes of the cancelled download become no-ops instead of overwriting
    /// <see cref="AppUpdateServiceState.Idle"/>.
    /// </summary>
    private readonly Lock _stateGate = new();

    private volatile int _operationGeneration;
    private CancellationTokenSource? _downloadCts;

    /// <summary>
    /// Completes once the running download operation has fully wound down, including disposing the
    /// downloader and releasing the downloaded file. Lets <see cref="CancelUpdateAsync"/> wait for the
    /// download to stop without ever blocking the UI thread.
    /// </summary>
    private TaskCompletionSource? _downloadCompletionTcs;

    private static readonly TimeSpan DownloadCancelWaitTimeout = TimeSpan.FromSeconds(15);

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

        // Everything the finishing continuation needs is captured in locals: CancelUpdateAsync clears the
        // service fields while the download is still winding down, so the continuation must never read them.
        var pathToDownloadFile = Path.Combine(AppStorageService.GetTempPath(), "update", "package");
        var downloadCts = new CancellationTokenSource();
        var cancellationToken = downloadCts.Token;
        var downloadResultTcs = new TaskCompletionSource();

        var download = DownloadBuilder.New()
            .WithUrl(platformInformation.Url)
            .WithFileLocation(pathToDownloadFile)
            .WithConfiguration(new DownloadConfiguration
            {
                ParallelCount = 8,
                ParallelDownload = true,
                EnableAutoResumeDownload = false
            })
            .WithHttpClient(() => httpClientFactory.CreateClient(nameof(AppUpdateService)))
            .Build();

        int generation;
        TaskCompletionSource downloadCompletionTcs;

        lock (_stateGate)
        {
            generation = ++_operationGeneration;

            _downloadCts = downloadCts;
            _pathToDownloadFile = pathToDownloadFile;
            downloadCompletionTcs = _downloadCompletionTcs = new TaskCompletionSource();

            UpdateInformation = updateInformation;
            LastException = null;
            BytesPerSecondSpeed = null;
            TotalFileSize = null;
            DownloadedFileSize = null;
            UpdateState = AppUpdateServiceState.Downloading;
        }

        RaiseStateChanged(AppUpdateServiceState.Downloading);

        download.DownloadProgressChanged += (_, args) =>
        {
            if (_operationGeneration != generation)
                return;

            BytesPerSecondSpeed = args.BytesPerSecondSpeed;
            TotalFileSize = download.TotalFileSize;
            DownloadedFileSize = download.DownloadedFileSize;
        };

        download.DownloadFileCompleted += (_, args) =>
        {
            // The downloader reports a user cancellation as an error as well, so the cancellation token has
            // to win here: otherwise cancelling the download is indistinguishable from a failed download.
            if (args.Cancelled || cancellationToken.IsCancellationRequested)
            {
                downloadResultTcs.TrySetCanceled(cancellationToken);
                return;
            }

            if (args.Error is { } ex)
            {
                downloadResultTcs.TrySetException(ex);
                return;
            }

            downloadResultTcs.TrySetResult();
        };

        _ = Task.Run(async () =>
        {
            try
            {
                await download.StartAsync(cancellationToken);
                await downloadResultTcs.Task; // faults with the real download error, if any

                cancellationToken.ThrowIfCancellationRequested();

                await using var fileStream = File.OpenRead(pathToDownloadFile);
                var fileSha256 = await ComputeSha256Async(fileStream, cancellationToken);
                var remoteSha256 = platformInformation.Sha256;

                if (!string.Equals(fileSha256, remoteSha256, StringComparison.OrdinalIgnoreCase))
                    throw new UpdateFileIntegrityCheckFailedException(fileSha256, remoteSha256);

                logger.LogInformation("Update downloaded and waiting for install");
                TrySetState(generation, AppUpdateServiceState.WaitingForInstall);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelling is a normal outcome and not a download error: CancelUpdateAsync owns the
                // state from here on, so the cancelled operation must not touch it.
                logger.LogInformation("Update download was canceled");
            }
            catch (UpdateFileIntegrityCheckFailedException ex)
            {
                logger.LogError(
                    "Downloaded file sha256 didn't match remote sha256, Remote: {RemoteSha256} Local: {FileSha256}",
                    ex.RemoteSha256, ex.LocalSha256
                );

                if (TrySetState(generation, AppUpdateServiceState.IntegrityCheckFailed))
                    NotifException(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to download update");

                if (TrySetState(generation, AppUpdateServiceState.DownloadError))
                    NotifException(ex);
            }
            finally
            {
                try
                {
                    await download.DisposeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to release the update downloader");
                }

                // Never leave CancelUpdateAsync waiting, whatever happened above.
                downloadCompletionTcs.TrySetResult();
            }
        });
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
            var pathToDownloadFile = _pathToDownloadFile;

            if (pathToDownloadFile is null)
            {
                Debug.Fail("_pathToDownloadFile should not be null when WaitingForInstall");
                throw new InvalidOperationException("_pathToDownloadFile should not be null when WaitingForInstall");
            }

            await updateInstallationService.InstallUpdateAsync(pathToDownloadFile);
            Dispatcher.UIThread.Invoke(lifetimeService.Shutdown);
        }
        catch (Exception ex)
        {
            NotifException(ex);
            SetState(AppUpdateServiceState.InstallError);
            throw;
        }
    }

    public async ValueTask RetryUpdateAsync()
    {
        // A visible retry button outlives some states the service cannot retry from (for example an install
        // failure), so this must never throw: an unhandled exception in the command would take the app down.
        if (UpdateState is not (AppUpdateServiceState.DownloadError or AppUpdateServiceState.IntegrityCheckFailed))
        {
            logger.LogWarning(
                "Retry was requested while the update service is not in a retryable state ({State}), ignoring",
                UpdateState);
            return;
        }

        if (UpdateInformation is not { } update)
        {
            logger.LogWarning("Retry was requested while no update information is available, ignoring");
            return;
        }

        await CancelUpdateAsync();
        StartDownloadUpdate(update);
    }

    public async ValueTask CancelUpdateAsync()
    {
        logger.LogInformation("Canceling update");

        int generation;
        CancellationTokenSource? downloadCts;
        TaskCompletionSource? downloadCompletionTcs;
        string? pathToDownloadFile;

        lock (_stateGate)
        {
            // Invalidate the running operation first: from now on none of its state changes apply.
            generation = ++_operationGeneration;

            downloadCts = _downloadCts;
            downloadCompletionTcs = _downloadCompletionTcs;
            pathToDownloadFile = _pathToDownloadFile;

            _downloadCts = null;
            _downloadCompletionTcs = null;
            _pathToDownloadFile = null;
            UpdateInformation = null;
        }

        if (downloadCts is not null)
        {
            try
            {
                await downloadCts.CancelAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cancel the update download");
            }
        }

        if (downloadCompletionTcs is not null)
        {
            // Wait (asynchronously) for the download to stop and release the downloaded file before
            // touching that file, and before the state goes back to Idle.
            try
            {
                await downloadCompletionTcs.Task.WaitAsync(DownloadCancelWaitTimeout);
            }
            catch (TimeoutException)
            {
                logger.LogWarning(
                    "Timed out after {Timeout} waiting for the canceled update download to stop",
                    DownloadCancelWaitTimeout);
            }
        }

        downloadCts?.Dispose();

        if (pathToDownloadFile is not null && File.Exists(pathToDownloadFile))
        {
            try
            {
                File.Delete(pathToDownloadFile);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cleanup downloaded file");
            }
        }

        lock (_stateGate)
        {
            // Reset only after the download stopped, so late progress events can't write them back.
            BytesPerSecondSpeed = null;
            DownloadedFileSize = null;
            TotalFileSize = null;
        }

        TrySetState(generation, AppUpdateServiceState.Idle);
    }

    /// <summary>
    /// Applies <paramref name="state"/> only when <paramref name="generation"/> still owns the service,
    /// so a cancelled download can never overwrite the state of the operation that replaced it.
    /// </summary>
    private bool TrySetState(int generation, AppUpdateServiceState state)
    {
        lock (_stateGate)
        {
            if (_operationGeneration != generation)
                return false;

            UpdateState = state;
        }

        RaiseStateChanged(state);
        return true;
    }

    private void SetState(AppUpdateServiceState state)
    {
        lock (_stateGate)
        {
            UpdateState = state;
        }

        RaiseStateChanged(state);
    }

    /// <summary>
    /// Raises <see cref="OnUpdateStateChanged"/> on the UI thread: the download continuation reports its
    /// result from a thread pool thread, while every subscriber updates UI bound state.
    /// </summary>
    private void RaiseStateChanged(AppUpdateServiceState state)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            OnUpdateStateChanged?.Invoke(this, state);
            return;
        }

        try
        {
            Dispatcher.UIThread.Post(() => OnUpdateStateChanged?.Invoke(this, state));
        }
        catch (Exception ex)
        {
            // The dispatcher is already gone (the app is shutting down). The state itself is updated, so
            // dropping the notification is safe.
            logger.LogWarning(ex, "Failed to post update state change to the UI thread");
        }
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