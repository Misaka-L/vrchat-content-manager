using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

namespace VRChatContentManager.Core.Services.PublishTask;

public sealed class TaskManagerService(ContentPublishTaskFactory contentPublishTaskFactory)
{
    public async ValueTask<ContentPublishTaskService> CreateTask(
        string contentId,
        string bundleFileId,
        IContentPublisher contentPublisher
    )
    {
        return await contentPublishTaskFactory.Create(contentId, bundleFileId, contentPublisher);
    }
}