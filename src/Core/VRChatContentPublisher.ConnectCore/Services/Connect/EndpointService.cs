using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Results;

namespace VRChatContentPublisher.ConnectCore.Services.Connect;

public sealed class EndpointService(ILogger<EndpointService> logger, IServiceProvider serviceProvider)
{
    private readonly Dictionary<EndpointInfo, Func<HttpContext, IServiceProvider, Task<IEndpointResult>>> _handlers = [];

    public void Map(string method, string path, Func<HttpContext, IServiceProvider, Task<IEndpointResult>> handler)
    {
        var key = new EndpointInfo(path, method.ToUpperInvariant());
        _handlers[key] = handler;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.ToString();
        var requestMethod = context.Request.Method.ToUpperInvariant();

        var key = new EndpointInfo(requestPath, requestMethod);
        if (_handlers.TryGetValue(key, out var handler))
        {
            IEndpointResult result;
            try
            {
                result = await handler(context, serviceProvider);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error handling request {Method} {Path}", requestMethod, requestPath);
                result = EndpointResults.Problem(ApiV1ProblemType.Undocumented,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error", "An unexpected error occurred.");
            }

            await result.ExecuteAsync(context);
            return;
        }

        if (_handlers.Any(pair => pair.Key.Path == requestPath))
        {
            await EndpointResults.Problem(ApiV1ProblemType.Undocumented,
                    StatusCodes.Status405MethodNotAllowed,
                    "Method Not Allowed", "The method is not allowed for the requested Endpoint.")
                .ExecuteAsync(context);
            return;
        }

        await EndpointResults.Problem(ApiV1ProblemType.Undocumented, StatusCodes.Status404NotFound,
                "Not Found", "The requested Endpoint was not found on the server.")
            .ExecuteAsync(context);
    }

    private record EndpointInfo(string Path, string Method);
}