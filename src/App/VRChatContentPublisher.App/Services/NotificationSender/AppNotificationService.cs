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

        await desktopNotificationService.SendDesktopNotificationAsync(title, message);
    }
}