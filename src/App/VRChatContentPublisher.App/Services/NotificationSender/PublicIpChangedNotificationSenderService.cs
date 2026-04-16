using Antelcat.I18N.Avalonia;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Services.PublicIp;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.NotificationSender;

public sealed class PublicIpChangedNotificationSenderService(
    IWritableOptions<AppSettings> appSettings,
    AppNotificationService appNotificationService,
    InAppNotificationService inAppNotificationService,
    PublicIpChangedInAppNotificationViewModelFactory notificationFactory,
    PublicIpCheckerService publicIpCheckerService,
    ISubscriber<PublicIpChangedEvent> ipChangedSubscriber)
    : IHostedService
{
    private IDisposable? _subscription;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TrySendInAppNotification();

        _subscription = ipChangedSubscriber.Subscribe(args =>
        {
            TrySendInAppNotification();

            if (!appSettings.Value.SendNotificationOnPublicIpChanged)
                return;

            var title = LangKeys.Notifications_Public_IP_Changed_Title;
            var message =
                string.Format(
                    I18NExtension.Translate(LangKeys.Notifications_Public_IP_Changed_Body_Template) ??
                    "Old: {0} New: {1}", args.OldIpPlaintext, args.NewIpPlaintext
                );

            _ = appNotificationService.SendNotificationAsync(title, message).AsTask();
        });

        return Task.CompletedTask;
    }

    private void TrySendInAppNotification()
    {
        if (publicIpCheckerService.IsWarningDismissed)
            return;

        if (inAppNotificationService.Notifications.Any(x => x is PublicIpChangedInAppNotificationViewModel))
            return;

        inAppNotificationService.SendNotification(notificationFactory.Create());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }
}