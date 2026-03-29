using Antelcat.I18N.Avalonia;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.Services.NotificationSender;

public sealed class AppNotificationService(
    IDesktopNotificationService desktopNotificationService,
    IWritableOptions<AppSettings> appSettings)
{
    public async ValueTask SendNotificationAsync(string title, string? message = null)
    {
        if (!appSettings.Value.NotificationsEnabled)
            return;

        var localizedTitle = I18NExtension.Translate(title) ?? title;
        var localizedMessage = message is not null ? I18NExtension.Translate(message, message) : message;

        await desktopNotificationService.SendDesktopNotificationAsync(localizedTitle, localizedMessage);
    }
}