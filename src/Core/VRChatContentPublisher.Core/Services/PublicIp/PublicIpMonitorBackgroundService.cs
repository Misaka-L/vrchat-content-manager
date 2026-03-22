using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.Core.Services.PublicIp;

public sealed class PublicIpMonitorBackgroundService(
    PublicIpCheckerService checkerService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber,
    ILogger<PublicIpMonitorBackgroundService> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _checkSignal = new(0);
    private IDisposable? _sessionInvalidatedSubscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _sessionInvalidatedSubscription = sessionStateChangedSubscriber.Subscribe(args =>
        {
            if (args.SessionState == UserSessionState.InvalidSession ||
                args is { OldSessionState: UserSessionState.InvalidSession, SessionState: UserSessionState.LoggedIn })
            {
                _checkSignal.Release();
            }
        });

        await RunCheckSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var timerTask = Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            var signalTask = _checkSignal.WaitAsync(stoppingToken);

            var completed = await Task.WhenAny(timerTask, signalTask);
            await completed;

            await RunCheckSafelyAsync(stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionInvalidatedSubscription?.Dispose();
        _sessionInvalidatedSubscription = null;
        return base.StopAsync(cancellationToken);
    }

    private async Task RunCheckSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await checkerService.RequestCheckAndPublishIfChangedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check internet public IP.");
        }
    }
}