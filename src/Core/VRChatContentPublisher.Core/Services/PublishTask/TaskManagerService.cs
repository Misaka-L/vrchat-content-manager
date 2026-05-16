using MessagePipe;
using VRChatContentPublisher.Core.Database;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;

namespace VRChatContentPublisher.Core.Services.PublishTask;

public sealed class TaskManagerService(
    ContentPublishTaskFactory contentPublishTaskFactory,
    ContentPublishTaskDatabaseService contentPublishTaskDatabaseService,
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

    /// <summary>
    /// Creates a new publish task from the given state model.
    /// A unique <see cref="ContentPublishTaskState.TaskId"/> will be generated automatically.
    /// The task state is persisted to the database immediately after creation.
    /// </summary>
    public async ValueTask<ContentPublishTaskService> CreateTask(
        ContentPublishTaskState state,
        IContentPublisher contentPublisher
    )
    {
        state.TaskId = Guid.NewGuid().ToString("D");

        var task = await contentPublishTaskFactory.CreateAsync(state, contentPublisher);

        RegisterTask(task);
        await contentPublishTaskDatabaseService.SaveStateAsync(task.State);
        return task;
    }

    /// <summary>
    /// Restores a publish task from a previously persisted state snapshot.
    /// Does not re-validate files or call <see cref="IContentPublisher.BeforePublishTaskAsync"/>,
    /// which is appropriate when resuming tasks after an application restart.
    /// The restored state is persisted to the database to update the task record.
    /// </summary>
    public async ValueTask<ContentPublishTaskService> RestoreTaskFromStateAsync(
        ContentPublishTaskState restoredState,
        IContentPublisher contentPublisher
    )
    {
        var task = await contentPublishTaskFactory.CreateFromStateAsync(restoredState, contentPublisher);

        RegisterTask(task);
        await contentPublishTaskDatabaseService.SaveStateAsync(task.State);
        return task;
    }

    private void RegisterTask(ContentPublishTaskService task)
    {
        _tasks.Add(task.TaskId, task);

        // Wire up explicit persistence: the task service actively requests
        // persistence on terminal state transitions, and we await it.
        task.PersistStateAsync = () =>
            new ValueTask(contentPublishTaskDatabaseService.SaveStateAsync(task.State));

        var args = new ContentPublishTaskCreatedEventArg(task);
        TaskCreated?.Invoke(this, args);
        taskCreatedPublisher.Publish(args);

        task.ProgressChanged += TaskOnProgressChanged;
    }

    public async ValueTask<bool> RemoveTaskAsync(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
            return false;

        if (task.Status != ContentPublishTaskStatus.Failed &&
            task.Status != ContentPublishTaskStatus.Completed &&
            task.Status != ContentPublishTaskStatus.Canceled &&
            task.Status != ContentPublishTaskStatus.Pending)
            return false;

        await task.CleanupAsync();

        _tasks.Remove(taskId);

        var args = new ContentPublishTaskRemovedEventArg(task);
        TaskRemoved?.Invoke(this, args);
        taskRemovedPublisher.Publish(args);

        await contentPublishTaskDatabaseService.DeleteStateAsync(taskId);
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