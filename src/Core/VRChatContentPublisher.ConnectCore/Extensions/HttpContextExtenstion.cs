using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace VRChatContentPublisher.ConnectCore.Extensions;

internal static class HttpContextExtenstion
{
    public static async ValueTask<TValue?> ReadJsonWithErrorHandleAsync<TValue>(this HttpContext httpContext,
        JsonTypeInfo<TValue> typeInfo)
    {
        try
        {
            return await httpContext.Request.ReadFromJsonAsync(typeInfo);
        }
        catch (Exception)
        {
            return default;
        }
    }
}