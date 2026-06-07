using Microsoft.AspNetCore.Http;

namespace VRChatContentPublisher.ConnectCore.Results;

public interface IEndpointResult
{
    Task ExecuteAsync(HttpContext context);
}
