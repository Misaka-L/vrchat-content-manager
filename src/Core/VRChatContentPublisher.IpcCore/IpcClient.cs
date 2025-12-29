using System.IO.Pipes;
using VRChatContentPublisher.IpcCore.Models;
using VRChatContentPublisher.IpcCore.Services;

namespace VRChatContentPublisher.IpcCore;

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