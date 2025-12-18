using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Extensions;
using VRChatContentManager.ConnectCore.Models.Api.V1;
using VRChatContentManager.ConnectCore.Services.Connect;
using VRChatContentManager.ConnectCore.Services.Health;

namespace VRChatContentManager.ConnectCore.Endpoints.V1;

public static class HealthEndpoint
{
    public static EndpointService MapHealthEndpoints(this EndpointService endpoints)
    {
        endpoints.Map("GET", "/v1/health/ready-for-publish", IsReadyForPublish);
        return endpoints;
    }

    private static async Task IsReadyForPublish(HttpContext httpContext, IServiceProvider services)
    {
        var contentPublishService = services.GetRequiredService<IHealthService>();
        var isReady = await contentPublishService.IsReadyForPublishAsync();

        if (isReady)
        {
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await httpContext.Response.WriteProblemAsync(ApiV1ProblemType.Undocumented,
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            "The service is not ready for publishing content at this time.");
    }
}