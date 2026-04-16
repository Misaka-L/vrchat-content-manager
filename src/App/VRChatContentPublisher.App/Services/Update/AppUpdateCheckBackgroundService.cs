using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.App.Services.NotificationSender;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckBackgroundService(
    AppUpdateCheckService updateCheckService,
    InAppNotificationService inAppNotificationService,
    AppNotificationService appNotificationService,
    UpdateAvailableAppNotificationViewModelFactory notificationFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var update = await updateCheckService.CheckForUpdateAsync();
        inAppNotificationService.SendNotification(notificationFactory.Create(update));
    }
}