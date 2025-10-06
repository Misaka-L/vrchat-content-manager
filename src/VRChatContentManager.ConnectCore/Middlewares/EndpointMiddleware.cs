using Microsoft.AspNetCore.Http;

namespace VRChatContentManager.ConnectCore.Middlewares;

public sealed class EndpointMiddleware : MiddlewareBase
{
    public override async Task ExecuteAsync(HttpContext context, Func<Task> next)
    {
        if (context.Request.Path == "/test-endpoint")
        {
            context.Response.StatusCode = 204;
            await next();
            return;
        }

        context.Response.StatusCode = 404;
        await next();
    }
}