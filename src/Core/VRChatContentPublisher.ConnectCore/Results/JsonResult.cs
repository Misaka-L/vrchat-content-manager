using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace VRChatContentPublisher.ConnectCore.Results;

public sealed class JsonResult<T> : IEndpointResult
{
    private readonly T _value;
    private readonly JsonTypeInfo<T> _typeInfo;
    private readonly int _statusCode;

    public JsonResult(T value, JsonTypeInfo<T> typeInfo, int statusCode = StatusCodes.Status200OK)
    {
        _value = value;
        _typeInfo = typeInfo;
        _statusCode = statusCode;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = _statusCode;
        return context.Response.WriteAsJsonAsync(_value, _typeInfo);
    }
}
