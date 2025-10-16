using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Services.PublishTask;
using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services.PublishTask.Connect;

public class WorldPublishTaskService(
    UserSessionManagerService userSessionManagerService,
    WorldContentPublisherFactory contentPublisherFactory) : IWorldPublishTaskService
{
    public async ValueTask<string> CreatePublishTaskAsync(string worldId, string worldBundleFileId, string platform,
        string unityVersion,
        string? worldSignature)
    {
        // TODO: Find user owned this content

        var userSession = userSessionManagerService.Sessions[0];
        var scope = await userSession.CreateOrGetSessionScopeAsync();

        var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
        var contentPublisher =
            contentPublisherFactory.Create(userSession, worldId, platform, unityVersion, worldSignature);

        var task = await taskManager.CreateTask(worldId, worldBundleFileId, contentPublisher);
        _ = task.StartTaskAsync().AsTask();

        return "task-id";
    }
}