using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

namespace VRChatContentManager.Core.Services.PublishTask;

public sealed class TaskManagerService(ContentPublishTaskFactory contentPublishTaskFactory)
{
    public IReadOnlyDictionary<string, ContentPublishTaskService> Tasks => _tasks.AsReadOnly();
    private readonly Dictionary<string, ContentPublishTaskService> _tasks = [];
    
    public event EventHandler<ContentPublishTaskService>? TaskCreated;
    
    public async ValueTask<ContentPublishTaskService> CreateTask(
        string contentId,
        string bundleFileId,
        IContentPublisher contentPublisher
    )
    {
        var taskId = Guid.NewGuid().ToString("D");
        var task = await contentPublishTaskFactory.Create(contentId, bundleFileId, contentPublisher);
        
        _tasks.Add(taskId, task);
        TaskCreated?.Invoke(this, task);
        return task;
    }
}