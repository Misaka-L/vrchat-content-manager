using Microsoft.Extensions.Logging;

namespace VRChatContentManager.Core.Services.VRChatApi;

public sealed class VRChatApiClientFactory(ILogger<VRChatApiClient> logger)
{
    public VRChatApiClient Create(HttpClient sessionClient)
    {
        return new VRChatApiClient(sessionClient, logger);
    }
}