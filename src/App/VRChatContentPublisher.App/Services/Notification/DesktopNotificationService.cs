using Antelcat.I18N.Avalonia;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.Services.Notification;

public sealed class DesktopNotificationService(
    ILogger<DesktopNotificationService> logger,
    IDesktopNotificationService desktopNotificationService,
    InAppNotificationService inAppNotificationService,
    IWritableOptions<AppSettings> appSettings)
{
    public bool IsSupported => desktopNotificationService.IsSupported;

    public bool IsInitialized { get; private set; }
    public Exception? LastInitializationException { get; private set; }

    public async ValueTask TryInitializeAsync()
    {
        if (!IsSupported || IsInitialized)
            return;

        try
        {
            await desktopNotificationService.InitializeAsync();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the desktop notification service");
            LastInitializationException = ex;

            inAppNotificationService.SendSimpleNotification(
                SimpleInAppNotificationType.Warning,
                LangKeys.In_App_Notifications_Desktop_Notification_Unavailable_Title,
                string.Format(
                    I18NExtension.Translate(LangKeys
                        .In_App_Notifications_Desktop_Notification_Unavailable_Body_Template) ??
                    "Error: {0}\nNOTE: Some users report that KB5083807 broke Windows notification service. You may need to uninstall this update in order to fix this."
                    , ex.Message)
            );
        }
    }

    public async ValueTask SendNotificationAsync(string title, string? message = null)
    {
        if (!appSettings.Value.NotificationsEnabled)
            return;

        if (!IsInitialized)
            return;

        var localizedTitle = I18NExtension.Translate(title) ?? title;
        var localizedMessage = message is not null ? I18NExtension.Translate(message, message) : message;

        await desktopNotificationService.SendDesktopNotificationAsync(localizedTitle, localizedMessage);
    }
}