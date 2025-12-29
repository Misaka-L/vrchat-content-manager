using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Services.VRChatApi.S3;

namespace VRChatContentPublisher.Core.Services.VRChatApi;

public sealed class VRChatApiClientFactory(
    ILogger<VRChatApiClient> logger,
    ConcurrentMultipartUploaderFactory uploaderFactory)
{
    public VRChatApiClient Create(HttpClient sessionClient)
    {
        return new VRChatApiClient(sessionClient, logger, uploaderFactory);
    }
}