using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Exceptions;
using VRChatContentPublisher.ConnectCore.Services.PublishTask;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher.Options;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.VRChatApi.Exceptions;

namespace VRChatContentPublisher.Core.Rpc.TaskCreation;

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
        string? authorId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus)
    {
        try
        {
            var userSession = await GetUserSessionByAvatarIdAsync(avatarId, authorId);
            var scope = await userSession.CreateOrGetSessionScopeAsync();

            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
            var contentPublisher =
                contentPublisherFactory.Create(userSession, new AvatarContentPublisherOptions
                {
                    AvatarId = avatarId,
                    Name = name,
                    Platform = platform,
                    UnityVersion = unityVersion
                });

            var state = new ContentPublishTaskState
            {
                ContentId = avatarId,
                RawBundleFileId = avatarBundleFileId,
                ThumbnailFileId = thumbnailFileId,
                Description = description,
                Tags = tags,
                ReleaseStatus = releaseStatus,
                UserId = userSession.UserId
            };

            var task = await taskManager.CreateTask(state, contentPublisher);
            task.Start();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create avatar publish task for avatar {AvatarId}", avatarId);
            throw;
        }
    }

    public async ValueTask<UserSessionService> GetUserSessionByAvatarIdAsync(string avatarId,
        string? requestUserId = null)
    {
        if (!userSessionManagerService.IsAnySessionAvailable)
            throw new NoUserSessionAvailableException();

        if (requestUserId is not null)
        {
            var requestSession = userSessionManagerService.Sessions
                .FirstOrDefault(session => session.UserId == requestUserId);

            if (requestSession is null)
                throw new ContentOwnerSessionOrAvatarNotFoundException();

            try
            {
                var avatar = await requestSession.GetApiClient().GetAvatarAsync(avatarId);
                if (avatar.AuthorId != requestUserId)
                    throw new ContentOwnerSessionOrAvatarNotFoundException();

                return requestSession;
            }
            catch (ApiErrorException ex) when (ex.StatusCode == 404)
            {
                throw new ContentOwnerSessionOrAvatarNotFoundException();
            }
        }

        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                var avatar = await session.GetApiClient().GetAvatarAsync(avatarId);
                if (avatar.AuthorId != session.UserId)
                    continue;

                return session;
            }
            catch
            {
                // ignored
            }
        }

        throw new ContentOwnerSessionOrAvatarNotFoundException();
    }
}