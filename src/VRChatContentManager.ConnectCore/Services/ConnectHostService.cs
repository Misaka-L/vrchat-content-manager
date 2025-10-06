using Microsoft.Extensions.Hosting;

namespace VRChatContentManager.ConnectCore.Services;

public class ConnectHostService(HttpServerService httpServerService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await httpServerService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await httpServerService.StopAsync(cancellationToken);
    }
}