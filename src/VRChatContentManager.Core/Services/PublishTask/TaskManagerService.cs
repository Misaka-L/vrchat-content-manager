namespace VRChatContentManager.Core.Services.PublishTask;

public sealed class TaskManagerService(ContentPublishTaskFactory contentPublishTaskFactory)
{
    public async ValueTask<ContentPublishTaskService> CreateTask(string contentId)
    {
        return await contentPublishTaskFactory.Create(contentId);
    }
}