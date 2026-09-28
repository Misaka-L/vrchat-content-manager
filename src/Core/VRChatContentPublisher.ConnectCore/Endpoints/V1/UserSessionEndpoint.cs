using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Results;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.ConnectCore.Services.UserSession;

namespace VRChatContentPublisher.ConnectCore.Endpoints.V1;

public static class UserSessionEndpoint
{
    public static EndpointService MapUserSessionEndpoints(this EndpointService endpoints)
    {
        endpoints.Map("GET", "/v1/user-sessions/validity", CheckSessionValidity);

        return endpoints;
    }

    private static async Task<IEndpointResult> CheckSessionValidity(HttpContext context, IServiceProvider services)
    {
        var userId = context.Request.Query["userId"].ToString().Trim();
        if (string.IsNullOrEmpty(userId))
            return EndpointResults.Problem(ApiV1ProblemType.Undocumented, StatusCodes.Status400BadRequest,
                "Bad Request", "The query parameter \"userId\" is required.");

        var validityService = services.GetRequiredService<IUserSessionValidityService>();
        var status = await validityService.CheckSessionValidityAsync(userId);

        return status switch
        {
            UserSessionValidityStatus.Valid => EndpointResults.NoContent(),
            UserSessionValidityStatus.Invalid => EndpointResults.Problem(ApiV1ProblemType.Undocumented,
                StatusCodes.Status503ServiceUnavailable,
                "Session Invalid",
                $"The VRChat account session for user {userId} is no longer valid. Please sign in again in the app."),
            _ => EndpointResults.Problem(ApiV1ProblemType.Undocumented, StatusCodes.Status404NotFound,
                "Session Not Found",
                $"No VRChat account session for user {userId} was found in the app.")
        };
    }
}