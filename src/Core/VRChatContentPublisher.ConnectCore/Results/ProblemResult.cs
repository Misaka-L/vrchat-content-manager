using Microsoft.AspNetCore.Http;
using VRChatContentPublisher.ConnectCore.Extensions;

namespace VRChatContentPublisher.ConnectCore.Results;

public sealed class ProblemResult : IEndpointResult
{
    private readonly string _type;
    private readonly int _statusCode;
    private readonly string _title;
    private readonly string? _detail;

    public ProblemResult(string type, int statusCode, string title, string? detail = null)
    {
        _type = type;
        _statusCode = statusCode;
        _title = title;
        _detail = detail;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        return context.Response.WriteProblemAsync(_type, _statusCode, _title, _detail);
    }
}
