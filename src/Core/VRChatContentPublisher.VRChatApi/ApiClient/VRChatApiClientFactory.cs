using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VRChatContentPublisher.VRChatApi.Models;

namespace VRChatContentPublisher.VRChatApi.ApiClient;

public sealed class VRChatApiClientFactory(
    ILogger<VRChatApiClient> logger,
    ConcurrentMultipartUploaderFactory uploaderFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<VRChatApiOptions> options,
    AppResiliencePipelineBuilderFactory resiliencePipelineBuilderFactory)
{
    public VRChatApiClient Create(HttpClient sessionClient)
    {
        return new VRChatApiClient(
            sessionClient,
            httpClientFactory,
            logger,
            uploaderFactory,
            options,
            resiliencePipelineBuilderFactory);
    }
}