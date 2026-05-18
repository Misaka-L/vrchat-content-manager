using VRChatContentPublisher.Core.ContentPublishing.PublishTask;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;

namespace VRChatContentPublisher.Core.Events.PublishTask;

public sealed class PublishTaskProgressChangedEvent(
    ContentPublishTaskService taskService,
    string progressText,
    double? progressValue,
    ContentPublishTaskStatus status
)
{
    public ContentPublishTaskService TaskService => taskService;

    public string ProgressText => progressText;
    public double? ProgressValue => progressValue;
    public ContentPublishTaskStatus Status => status;
}