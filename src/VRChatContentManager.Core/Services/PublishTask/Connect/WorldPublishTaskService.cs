using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Services.PublishTask;
using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services.PublishTask.Connect;

public class WorldPublishTaskService(
    UserSessionManagerService userSessionManagerService,
    WorldContentPublisherFactory contentPublisherFactory) : IWorldPublishTaskService
{
    public async ValueTask<string> CreatePublishTaskAsync(string worldId,
        string worldBundleFileId,
        string worldName,
        string platform,
        string unityVersion,
        string? worldSignature,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus)
    {
        var userSession = await GetUserSessionByWorldIdAsync(worldId);
        var scope = await userSession.CreateOrGetSessionScopeAsync();

        var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
        var contentPublisher =
            contentPublisherFactory.Create(userSession, worldId, worldName, platform, unityVersion, worldSignature);

        var task = await taskManager.CreateTask(
            worldId,
            worldBundleFileId,
            thumbnailFileId,
            description,
            tags,
            releaseStatus,
            contentPublisher);
        task.Start();

        return "task-id";
    }

    public async ValueTask<UserSessionService> GetUserSessionByWorldIdAsync(string worldId)
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                var world = await session.GetApiClient().GetWorldAsync(worldId);
                if (world.AuthorId != session.UserId)
                    continue;

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