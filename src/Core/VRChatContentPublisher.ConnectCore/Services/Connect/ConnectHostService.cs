using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.ConnectCore.Extensions;

namespace VRChatContentPublisher.ConnectCore.Services.Connect;

public class ConnectHostService(HttpServerService httpServerService, EndpointService endpointService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        endpointService.MapConnectService();
        
        await httpServerService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await httpServerService.StopAsync(cancellationToken);
    }
}