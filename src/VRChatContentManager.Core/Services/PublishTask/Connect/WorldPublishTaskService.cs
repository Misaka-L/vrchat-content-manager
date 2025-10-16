using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Services.PublishTask;
using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services.PublishTask.Connect;

public class WorldPublishTaskService(
    UserSessionManagerService userSessionManagerService,
    WorldContentPublisherFactory contentPublisherFactory) : IWorldPublishTaskService
{
    public async ValueTask<string> CreatePublishTaskAsync(
        string worldId,
        string worldBundleFileId,
        string worldName,
        string platform,
        string unityVersion,
        string? worldSignature)
    {
        var userSession = await GetUserSessionByWorldIdAsync(worldId);
        var scope = await userSession.CreateOrGetSessionScopeAsync();

        var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
        var contentPublisher =
            contentPublisherFactory.Create(userSession, worldId, worldName, platform, unityVersion, worldSignature);

        var task = await taskManager.CreateTask(worldId, worldBundleFileId, contentPublisher);
        _ = task.StartTaskAsync().AsTask();

        return "task-id";
    }

    public async ValueTask<UserSessionService> GetUserSessionByWorldIdAsync(string worldId)
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                await session.GetApiClient().GetWorldAsync(worldId);
                return session;
            }
            catch
            {
                // ignored
            }
        }

        throw new Exception("User session not found for the given world ID");
    }
}