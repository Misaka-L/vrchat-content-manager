namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public interface IContentPublisher
{
    ValueTask PublishAsync(Stream bundleFileStream, HttpClient awsClient);
}