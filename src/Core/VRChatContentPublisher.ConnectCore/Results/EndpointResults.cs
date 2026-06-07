using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace VRChatContentPublisher.ConnectCore.Results;

public static class EndpointResults
{
    public static JsonResult<T> Json<T>(T value, JsonTypeInfo<T> typeInfo,
        int statusCode = StatusCodes.Status200OK)
        => new(value, typeInfo, statusCode);

    public static ProblemResult Problem(string type, int statusCode, string title,
        string? detail = null)
        => new(type, statusCode, title, detail);

    public static StatusCodeResult StatusCode(int statusCode)
        => new(statusCode);

    public static StatusCodeResult NoContent()
        => new(StatusCodes.Status204NoContent);
}
