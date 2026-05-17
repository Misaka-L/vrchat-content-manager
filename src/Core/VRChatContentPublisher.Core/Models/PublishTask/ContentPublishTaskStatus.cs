namespace VRChatContentPublisher.Core.Models.PublishTask;

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