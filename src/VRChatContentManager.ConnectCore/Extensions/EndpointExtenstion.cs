using Microsoft.AspNetCore.Http;
using VRChatContentManager.ConnectCore.Services;

namespace VRChatContentManager.ConnectCore.Extensions;

public static class EndpointExtenstion
{
    public static EndpointService MapConnectService(this EndpointService endpointService)
    {
        endpointService.Map("GET", "/api/status", async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":\"ok\"}");
        });
        
        return endpointService;
    }
}