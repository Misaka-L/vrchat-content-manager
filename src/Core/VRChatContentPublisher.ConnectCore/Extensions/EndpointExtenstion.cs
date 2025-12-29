using VRChatContentPublisher.ConnectCore.Endpoints.V1;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.ConnectCore.Extensions;

public static class EndpointExtenstion
{
    public static EndpointService MapConnectService(this EndpointService endpointService)
    {
        endpointService.MapMetadataEndpoint();
        endpointService.MapAuthEndpoints();
        endpointService.MapFileEndpoints();
        endpointService.MapTaskEndpoint();
        endpointService.MapHealthEndpoints();

        return endpointService;
    }
}