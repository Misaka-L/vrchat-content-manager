using Microsoft.AspNetCore.Http;

namespace VRChatContentPublisher.ConnectCore.Results;

public sealed class StatusCodeResult : IEndpointResult
{
    private readonly int _statusCode;

    public StatusCodeResult(int statusCode)
    {
        _statusCode = statusCode;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = _statusCode;
        return Task.CompletedTask;
    }
}
