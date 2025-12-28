using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;

namespace VRChatContentPublisher.ConnectCore.Extensions;

internal static class ResponseExtenstion
{
    public static async Task WriteProblemAsync(this HttpResponse response,
        string type,
        int statusCode,
        string title,
        string? detail = null
    )
    {
        response.StatusCode = statusCode;

        await response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = detail
        }, ApiV1JsonContext.Default.ProblemDetails, "application/problem+json");
    }
}