using Microsoft.Extensions.Logging;

namespace VRChatContentPublisher.Core;

public sealed class ScopeLoggingHttpHandler(ILogger logger, string clientInstanceName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using (logger.BeginScope("Request sent from {HttpClientInstanceName}", clientInstanceName))
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}