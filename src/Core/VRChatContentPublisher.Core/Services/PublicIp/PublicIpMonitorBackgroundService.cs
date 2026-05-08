using MessagePipe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.PublicIp;

public sealed class PublicIpMonitorBackgroundService(
    PublicIpCheckerService checkerService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber,
    IWritableOptions<AppSettings> appSettings,
    ILogger<PublicIpMonitorBackgroundService> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _checkSignal = new(0);
    private IDisposable? _sessionInvalidatedSubscription;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!appSettings.Value.EnablePublicIpMonitor)
        {
            logger.LogInformation("Public IP monitor is disabled by user settings.");
            await WaitForCancellationAsync(stoppingToken);
            return;
        }

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

    private static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }
}