using Microsoft.AspNetCore.Http;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.ConnectCore.Middlewares;

public sealed class EndpointMiddleware(EndpointService endpointService) : MiddlewareBase
{
    public override async Task ExecuteAsync(HttpContext context, Func<Task> next)
    {
        await endpointService.HandleAsync(context);
        
        await next();
    }
}