using Microsoft.Extensions.Hosting;

namespace VRChatContentPublisher.App.Services.Notification;

public sealed class AppNotificationHostedService(
    DesktopNotificationService desktopNotificationService
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await desktopNotificationService.TryInitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}