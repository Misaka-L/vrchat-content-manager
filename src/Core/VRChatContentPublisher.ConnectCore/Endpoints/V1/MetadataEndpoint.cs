using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Models.Api.V1.Responses.Meta;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;

namespace VRChatContentPublisher.ConnectCore.Endpoints.V1;

public static class MetadataEndpoint
{
    public static EndpointService MapMetadataEndpoint(this EndpointService endpointService)
    {
        endpointService.Map("GET", "/v1/meta", async (context, services) =>
        {
            var metadataService = services.GetRequiredService<ConnectMetadataService>();
            await context.Response.WriteAsJsonAsync(new ApiV1MetadataResponse
            {
                ApiVersion = metadataService.GetApiVersion(),
                Implementation = metadataService.GetImplementation(),
                ImplementationVersion = metadataService.GetImplementationVersion(),
                InstanceName = metadataService.GetInstanceName(),
                FeatureFlags = metadataService.GetFeatureFlags()
            }, ApiV1JsonContext.Default.ApiV1MetadataResponse);
        });

        return endpointService;
    }
}