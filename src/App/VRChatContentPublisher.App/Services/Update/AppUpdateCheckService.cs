using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckService(
    ILogger<AppUpdateCheckService> logger,
    IWritableOptions<AppSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    InAppNotificationService inAppNotificationService,
    UpdateAvailableAppNotificationViewModelFactory notificationFactory,
    ConfirmUpdateDialogViewModelFactory dialogFactory,
    DialogService dialogService
)
{
    private const string StableChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater.json";

    private const string PreviewChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater-beta.json";

    public async ValueTask<AppUpdateInformation> CheckForUpdateAsync(bool isManual = false)
    {
        var update = await GetUpdateInformationAsync();

        if (update.Version == AppVersionUtils.GetAppVersion())
        {
            logger.LogInformation("Current version {Version} is up to date, no update available", update.Version);
            return update;
        }

        if (!isManual && update.Version == appSettings.Value.SkipVersion)
        {
            logger.LogInformation("Skip Version {Version} is set, skipping update notification", update.Version);
            return update;
        }

        logger.LogInformation("New version {Version} is available", update.Version);
        inAppNotificationService.SendNotification(notificationFactory.Create(update));

        if (isManual)
        {
            _ = dialogService.ShowDialogAsync(dialogFactory.Create(update)).AsTask();
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