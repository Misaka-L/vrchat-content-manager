using Microsoft.Extensions.Logging;
using VRChatContentPublisher.IpcCore.Models;

namespace VRChatContentPublisher.IpcCore.Services;

public sealed class IpcCommandService(ILogger<IpcCommandService> logger, IActivateWindowService activateWindowService)
{
    public async ValueTask HandleCommandAsync(Stream input, CancellationToken cancellationToken = default)
    {
        var reader = new BinaryReader(input);
        var command = (IpcCommand)reader.ReadInt32();

        logger.LogInformation("Received IPC command: {Command}", command);
        switch (command)
        {
            case IpcCommand.ActivateWindow:
                await activateWindowService.ActivateMainWindowAsync();
                break;
            default:
                logger.LogWarning("Received unknown IPC command: {Command}", command);
                break;
        }
    }
}