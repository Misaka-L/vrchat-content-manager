using System.Net.Http.Json;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckService(
    ILogger<AppUpdateCheckService> logger,
    IWritableOptions<AppSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    AppUpdateService appUpdateService
)
{
    private const string StableChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater.json";

    private const string PreviewChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater-beta.json";

    public event EventHandler<AppUpdateInformation>? OnUpdateAvailableToDownload;

    public async ValueTask<AppUpdateInformation?> CheckForUpdateAsync()
    {
        if (appUpdateService.UpdateState != AppUpdateServiceState.Idle)
            return null;

        var update = await GetUpdateInformationAsync();

        if (update.Version == AppVersionUtils.GetAppVersion())
        {
            logger.LogInformation("Current version {Version} is up to date, no update available", update.Version);
            return null;
        }

        if (update.Version == appSettings.Value.SkipVersion)
        {
            logger.LogInformation("Skip Version {Version} is set, skipping update notification", update.Version);
            return null;
        }

        logger.LogInformation("New version {Version} is available", update.Version);

        if (appSettings.Value.DownloadUpdateAtBackground)
        {
            logger.LogInformation("Starting download update at background");
            appUpdateService.StartDownloadUpdate(update);
        }
        else
        {
            OnUpdateAvailableToDownload?.Invoke(this, update);
        }

        return update;
    }

    private async ValueTask<AppUpdateInformation> GetUpdateInformationAsync()
    {
        using var httpClient = httpClientFactory.CreateClient();
        var information = await httpClient.GetFromJsonAsync<AppUpdateInformation>(
            GetUpdateApiUrl(),
            AppUpdateInformationJsonContext.Default.AppUpdateInformation
        ) ?? throw new UnexpectedApiBehaviourException("Update api returned null response");

        return information;
    }

    private string GetUpdateApiUrl()
    {
        return appSettings.Value.ReceivePreviewUpdate ? PreviewChannelUpdateApiUrl : StableChannelUpdateApiUrl;
    }
}