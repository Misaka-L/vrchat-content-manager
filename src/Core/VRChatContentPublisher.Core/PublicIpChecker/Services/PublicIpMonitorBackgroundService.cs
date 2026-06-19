using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.Core.PublicIpChecker.Services;

public sealed class PublicIpMonitorBackgroundService(
    PublicIpCheckerService checkerService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber,
    ISubscriber<RequestBackgroundPublicIpCheckRunEvent> requestRunSubscriber,
    IWritableOptions<AppSettings> appSettings,
    ILogger<PublicIpMonitorBackgroundService> logger)
    : BackgroundService
{
    private readonly AsyncAutoResetEvent _checkSignal = new();
    private IDisposable? _eventSubscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventSubscription = DisposableBag.Create(
            sessionStateChangedSubscriber.Subscribe(args =>
            {
                if (args.SessionState == UserSessionState.InvalidSession || args is
                    {
                        // Invalid before, logged-in now
                        OldSessionState: UserSessionState.InvalidSession, SessionState: UserSessionState.LoggedIn
                    })
                {
                    _checkSignal.Set();
                }
            }),
            requestRunSubscriber.Subscribe(_ => _checkSignal.Set())
        );

        await RunCheckSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var timerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timerCts.CancelAfter(TimeSpan.FromMinutes(30));

            try
            {
                await _checkSignal.WaitAsync(timerCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;
            }

            if (!appSettings.Value.EnablePublicIpMonitor)
            {
                logger.LogInformation("Public IP monitor has been disabled, skipping check.");
                continue;
            }

            await RunCheckSafelyAsync(stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _eventSubscription?.Dispose();
        _eventSubscription = null;
        return base.StopAsync(cancellationToken);
    }

    private async Task RunCheckSafelyAsync(CancellationToken cancellationToken)
    {
        if (!appSettings.Value.EnablePublicIpMonitor)
            return;
        
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