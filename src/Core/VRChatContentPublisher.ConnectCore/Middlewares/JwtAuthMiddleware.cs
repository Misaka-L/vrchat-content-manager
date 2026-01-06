using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.ConnectCore.Middlewares;

public class JwtAuthMiddleware(ClientSessionService clientSessionService, ILogger<JwtAuthMiddleware> logger)
    : MiddlewareBase
{
    public override async Task ExecuteAsync(HttpContext context, Func<Task> next)
    {
        if (context.Request.Path.StartsWithSegments("/v1/auth/challenge") ||
            context.Request.Path.StartsWithSegments("/v1/auth/request-challenge") ||
            context.Request.Path.StartsWithSegments("/v1/meta"))
        {
            await next();
            return;
        }

        var jwtHeader = context.Request.Headers.Authorization.ToString();
        if (!jwtHeader.StartsWith("Bearer "))
        {
            await WriteUnauthorizedAsync(context.Response);
            return;
        }

        var jwt = jwtHeader["Bearer ".Length..].Trim();
        var validateResult = await clientSessionService.ValidateJwtAsync(jwt);
        if (!validateResult.TokenValidationResult.IsValid)
        {
            await WriteUnauthorizedAsync(context.Response);
            return;
        }

        logger.BeginScope(
            "Valid JWT for {ClientName} ({ClientId})",
            validateResult.ClientName,
            validateResult.ClientId
        );

        context.User = new ClaimsPrincipal(validateResult.TokenValidationResult.ClaimsIdentity);
        await next();
    }

    private async Task WriteUnauthorizedAsync(HttpResponse response)
    {
        await response.WriteProblemAsync(ApiV1ProblemType.Undocumented, StatusCodes.Status401Unauthorized,
            "Invalid Session",
            "The session is invalid or has expired. Please authenticate again.");
    }
}