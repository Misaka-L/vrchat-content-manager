using System.IO.Pipes;
using VRChatContentManager.IpcCore.Models;
using VRChatContentManager.IpcCore.Services;

namespace VRChatContentManager.IpcCore;

public sealed class IpcClient
{
    public void SendIpcCommand(IpcCommand command)
    {
        using var clientNamedPipeStream =
            new NamedPipeClientStream(".", NamedPipeService.PipeName, PipeDirection.Out);

        clientNamedPipeStream.Connect(1000);

        using var writer = new BinaryWriter(clientNamedPipeStream);
        writer.Write((int)command);
    }
}