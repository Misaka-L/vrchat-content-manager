using Microsoft.Extensions.Http.Resilience;

namespace VRChatContentPublisher.Core.Resilience;

public class AppHttpRetryStrategyOptions : HttpRetryStrategyOptions
{
    public AppHttpRetryStrategyOptions()
    {
        ShouldHandle = args =>
            new ValueTask<bool>(
                AppHttpClientResiliencePredicates.IsTransient(args.Outcome, args.Context.CancellationToken));
    }
}