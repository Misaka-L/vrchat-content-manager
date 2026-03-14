using Microsoft.Extensions.Hosting;

namespace VRChatContentPublisher.Platform.Windows.Services;

internal sealed class WindowsHostedService(
    WindowsDesktopNotificationService desktopNotificationService
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        desktopNotificationService.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}