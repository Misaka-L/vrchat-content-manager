namespace VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;

public enum ContentPublishTaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelling,
    Canceled,
    Disposed
}