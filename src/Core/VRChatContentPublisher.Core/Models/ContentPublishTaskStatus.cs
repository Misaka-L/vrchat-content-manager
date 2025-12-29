namespace VRChatContentPublisher.Core.Models;

public enum ContentPublishTaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelling,
    Canceled
}