using MessagePipe;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.NotificationSender;

public sealed class PublicIpChangedNotificationSenderService(
    IWritableOptions<AppSettings> appSettings,
    AppNotificationService appNotificationService,
    ISubscriber<PublicIpChangedEvent> ipChangedSubscriber)
    : IHostedService
{
    private IDisposable? _subscription;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = ipChangedSubscriber.Subscribe(args =>
        {
            if (!appSettings.Value.SendNotificationOnPublicIpChanged)
                return;

            var title = "Public IP Changed";
            var message = $"Old: {args.OldIpPlaintext}  New: {args.NewIpPlaintext}";

            _ = appNotificationService.SendNotificationAsync(title, message).AsTask();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }
}

