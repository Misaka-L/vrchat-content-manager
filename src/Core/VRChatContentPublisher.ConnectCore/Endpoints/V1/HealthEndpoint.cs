using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Results;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.ConnectCore.Services.Health;

namespace VRChatContentPublisher.ConnectCore.Endpoints.V1;

public static class HealthEndpoint
{
    public static EndpointService MapHealthEndpoints(this EndpointService endpoints)
    {
        endpoints.Map("GET", "/v1/health/ready-for-publish", IsReadyForPublish);
        return endpoints;
    }

    private static async Task<IEndpointResult> IsReadyForPublish(HttpContext httpContext, IServiceProvider services)
    {
        var contentPublishService = services.GetRequiredService<IHealthService>();
        var isReady = await contentPublishService.IsReadyForPublishAsync();

        if (isReady)
        {
            return EndpointResults.NoContent();
        }

        return EndpointResults.Problem(ApiV1ProblemType.Undocumented,
            StatusCodes.Status503ServiceUnavailable,
            "Service Unavailable",
            "The service is not ready for publishing content at this time.");
    }
}