using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;

namespace VRChatContentPublisher.App.Services.NotificationSender;

public sealed class AppUpdateNotificationSender(
    AppUpdateService appUpdateService,
    AppUpdateCheckService appUpdateCheckService,
    InAppNotificationService inAppNotificationService,
    UpdateAvailableAppNotificationViewModelFactory updateAvailableNotificationFactory
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        appUpdateCheckService.OnUpdateAvailableToDownload += (_, update) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                inAppNotificationService.RemoveNotificationOfType<UpdateAvailableAppNotificationViewModel>();
                inAppNotificationService.SendNotification(updateAvailableNotificationFactory.Create(update));
            });
        };

        appUpdateService.OnUpdateStateChanged += (_, state) =>
        {
            if (state == AppUpdateServiceState.Idle)
            {
                inAppNotificationService.RemoveNotificationOfType<UpdateAvailableAppNotificationViewModel>();
                inAppNotificationService.RemoveNotificationOfType<UpdateProgressAppNotificationViewModel>();
                return;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                inAppNotificationService.RemoveNotificationOfType<UpdateAvailableAppNotificationViewModel>();

                if (inAppNotificationService.Notifications.Any(x => x is UpdateProgressAppNotificationViewModel))
                    return;

                inAppNotificationService.SendNotification<UpdateProgressAppNotificationViewModel>();
            });
        };

        return Task.CompletedTask;
    }
}