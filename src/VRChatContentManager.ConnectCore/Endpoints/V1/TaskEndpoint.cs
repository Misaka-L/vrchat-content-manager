using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Extensions;
using VRChatContentManager.ConnectCore.Models.Api.V1;
using VRChatContentManager.ConnectCore.Services.Connect;
using VRChatContentManager.ConnectCore.Services.PublishTask;

namespace VRChatContentManager.ConnectCore.Endpoints.V1;

public static class TaskEndpoint
{
    public static EndpointService MapTaskEndpoint(this EndpointService service)
    {
        service.Map("POST", "/v1/tasks/world", CreateWorldPublishTask);

        return service;
    }

    private static async Task CreateWorldPublishTask(HttpContext context, IServiceProvider services)
    {
        if (await context.ReadJsonWithErrorHandleAsync(ApiV1JsonContext.Default.CreateWorldPublishTaskRequest) is not
            { } request)
            return;

        var worldPublishTaskService = services.GetRequiredService<IWorldPublishTaskService>();
        await worldPublishTaskService.CreatePublishTaskAsync(request.WorldId, request.WorldBundleFileId, request.Platform,
            request.UnityVersion, request.WorldSignature);
    }
}