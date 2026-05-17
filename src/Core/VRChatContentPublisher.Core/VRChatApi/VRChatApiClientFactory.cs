using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.VRChatApi.S3;

namespace VRChatContentPublisher.Core.VRChatApi;

public sealed class VRChatApiClientFactory(
    ILogger<VRChatApiClient> logger,
    ConcurrentMultipartUploaderFactory uploaderFactory,
    IHttpClientFactory httpClientFactory)
{
    public VRChatApiClient Create(HttpClient sessionClient)
    {
        return new VRChatApiClient(sessionClient, httpClientFactory, logger, uploaderFactory);
    }
}