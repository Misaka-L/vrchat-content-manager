using Antelcat.I18N.Avalonia;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.PublicIpChecker.Services;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.Notification.Sender;

public sealed class PublicIpChangedNotificationSenderService(
    IWritableOptions<AppSettings> appSettings,
    DesktopNotificationService desktopNotificationService,
    InAppNotificationService inAppNotificationService,
    PublicIpChangedInAppNotificationViewModelFactory notificationFactory,
    PublicIpCheckerService publicIpCheckerService)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        TrySendInAppNotification();

        publicIpCheckerService.PublicIpChanged += OnPublicIpChanged;

        return Task.CompletedTask;
    }

    private void OnPublicIpChanged(object? sender, PublicIpChangedEvent args)
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

        _ = desktopNotificationService.SendNotificationAsync(title, message).AsTask();
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
        return Task.CompletedTask;
    }
}