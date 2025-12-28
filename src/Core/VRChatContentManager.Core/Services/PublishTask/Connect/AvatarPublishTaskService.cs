using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentManager.ConnectCore.Exceptions;
using VRChatContentManager.ConnectCore.Services.PublishTask;
using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services.PublishTask.Connect;

public sealed class AvatarPublishTaskService(
    AvatarContentPublisherFactory contentPublisherFactory,
    UserSessionManagerService userSessionManagerService,
    ILogger<AvatarPublishTaskService> logger)
    : IAvatarPublishTaskService
{
    public async Task CreatePublishTaskAsync(string avatarId,
        string avatarBundleFileId,
        string name,
        string platform,
        string unityVersion,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus)
    {
        try
        {
            var userSession = await GetUserSessionByAvatarIdAsync(avatarId);
            var scope = await userSession.CreateOrGetSessionScopeAsync();

            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
            var contentPublisher =
                contentPublisherFactory.Create(userSession, avatarId, name, platform, unityVersion);

            var task = await taskManager.CreateTask(avatarId, avatarBundleFileId, thumbnailFileId, description, tags,
                releaseStatus, contentPublisher);
            task.Start();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create avatar publish task for avatar {AvatarId}", avatarId);
            throw;
        }
    }

    public async ValueTask<UserSessionService> GetUserSessionByAvatarIdAsync(string avatarId)
    {
        if (!userSessionManagerService.IsAnySessionAvailable)
            throw new NoUserSessionAvailableException();

        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                var world = await session.GetApiClient().GetAvatarAsync(avatarId);
                if (world.AuthorId != session.UserId)
                    continue;

                return session;
            }
            catch
            {
                // ignored
            }
        }

        throw new ContentOwnerUserSessionNotFoundException();
    }
}