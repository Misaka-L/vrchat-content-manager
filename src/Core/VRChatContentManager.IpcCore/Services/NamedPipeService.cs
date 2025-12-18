using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using VRChatContentManager.IpcCore.Models;

namespace VRChatContentManager.IpcCore.Services;

public sealed class NamedPipeService(ILogger<NamedPipeService> logger, IpcCommandService ipcCommandService)
{
    public const string PipeName = "VRChatContentPublisherPipe-89c6689a";

    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _loopCts;

    public async ValueTask StartAsync()
    {
        if (_loopCts is not null)
            await _loopCts.CancelAsync();

        _loopCts = new CancellationTokenSource();

        _pipeServer = new NamedPipeServerStream(
            PipeName,
            PipeDirection.In,
            1);

        _ = Task.Factory.StartNew(NamedPipeServerLoop, TaskCreationOptions.LongRunning);
    }

    public async ValueTask StopAsync()
    {
        if (_loopCts is not null)
        {
            await _loopCts.CancelAsync();
            _loopCts.Dispose();
        }
    }

    private async Task NamedPipeServerLoop()
    {
        if (_loopCts is null)
        {
            logger.LogError("Named pipe server loop started without a valid cancellation token source.");
            return;
        }

        if (_pipeServer is null)
        {
            logger.LogError("Named pipe server loop started without a valid pipe server.");
            return;
        }

        var cancellationToken = _loopCts.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _pipeServer.WaitForConnectionAsync(cancellationToken);
                try
                {
                    await ipcCommandService.HandleCommandAsync(_pipeServer, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to handle IPC command.");
                }

                _pipeServer.Disconnect();
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Named pipe server loop failed.");
        }
        finally
        {
            await _pipeServer.DisposeAsync();
        }
    }
}