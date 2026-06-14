using Microsoft.Extensions.Hosting;

namespace VRChatContentPublisher.App.Services.Telemetry.PrivacyPolicy;

/// <summary>
/// Hosted service that triggers prefetching of the latest privacy policy
/// in parallel with app startup. Follows the same pattern as
/// <see cref="Update.AppUpdateCheckBackgroundService"/>.
/// </summary>
public sealed class PrivacyPolicyPrefetchHostedService(
    PrivacyPolicyService privacyPolicyService
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        privacyPolicyService.StartPrefetch();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
