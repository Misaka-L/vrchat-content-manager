using MessagePipe;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;

namespace VRChatContentPublisher.Core.Services.PublishTask;

public sealed class TaskManagerService(
    ContentPublishTaskFactory contentPublishTaskFactory,
    IPublisher<ContentPublishTaskUpdateEventArg> taskUpdatedPublisher,
    IPublisher<ContentPublishTaskCreatedEventArg> taskCreatedPublisher,
    IPublisher<ContentPublishTaskRemovedEventArg> taskRemovedPublisher
)
{
    public IReadOnlyDictionary<string, ContentPublishTaskService> Tasks => _tasks.AsReadOnly();
    private readonly Dictionary<string, ContentPublishTaskService> _tasks = [];

    public event EventHandler<ContentPublishTaskCreatedEventArg>? TaskCreated;
    public event EventHandler<ContentPublishTaskRemovedEventArg>? TaskRemoved;

    public event EventHandler<ContentPublishTaskUpdateEventArg>? TaskUpdated;

    public async ValueTask<ContentPublishTaskService> CreateTask(
        string contentId,
        string bundleFileId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        IContentPublisher contentPublisher
    )
    {
        var taskId = Guid.NewGuid().ToString("D");

        var task = await contentPublishTaskFactory.CreateAsync(taskId,
            contentId, bundleFileId, thumbnailFileId, description, tags, releaseStatus,
            contentPublisher);

        _tasks.Add(taskId, task);

        var args = new ContentPublishTaskCreatedEventArg(task);
        TaskCreated?.Invoke(this, args);
        taskCreatedPublisher.Publish(args);

        task.ProgressChanged += TaskOnProgressChanged;
        return task;
    }

    public async ValueTask<bool> RemoveTaskAsync(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return false;

        if (task.Status != ContentPublishTaskStatus.Failed &&
            task.Status != ContentPublishTaskStatus.Completed &&
            task.Status != ContentPublishTaskStatus.Canceled)
            return false;

        await task.CleanupAsync();

        _tasks.Remove(taskId);

        var args = new ContentPublishTaskRemovedEventArg(task);
        TaskRemoved?.Invoke(this, args);
        taskRemovedPublisher.Publish(args);
        return true;
    }

    private void TaskOnProgressChanged(object? sender, PublishTaskProgressEventArg e)
    {
        if (sender is not ContentPublishTaskService task)
            return;

        var args = new ContentPublishTaskUpdateEventArg(task, e);
        TaskUpdated?.Invoke(this, args);
        taskUpdatedPublisher.Publish(args);
    }
}

public record ContentPublishTaskUpdateEventArg(
    ContentPublishTaskService Task,
    PublishTaskProgressEventArg ProgressEventArg);

public record ContentPublishTaskCreatedEventArg(ContentPublishTaskService Task);
public record ContentPublishTaskRemovedEventArg(ContentPublishTaskService Task);