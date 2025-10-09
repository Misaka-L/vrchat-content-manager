using Microsoft.AspNetCore.Http;
using VRChatContentManager.ConnectCore.Models.Api.V1;
using VRChatContentManager.ConnectCore.Models.Api.V1.Responses.Meta;
using VRChatContentManager.ConnectCore.Services;

namespace VRChatContentManager.ConnectCore.Endpoints.V1;

public static class MetadataEndpoint
{
    public static EndpointService MapMetadataEndpoint(this EndpointService endpointService)
    {
        endpointService.Map("GET", "/v1/meta", async (context, _) =>
        {
            await context.Response.WriteAsJsonAsync(new ApiV1MetadataResponse(), ApiV1JsonContext.Default.ApiV1MetadataResponse);
        });

        return endpointService;
    }
}