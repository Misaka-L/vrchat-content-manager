using Microsoft.Extensions.Hosting;

namespace VRChatContentManager.IpcCore.Services;

public sealed class IpcHostedService(NamedPipeService namedPipeService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await namedPipeService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await namedPipeService.StopAsync();
    }
}