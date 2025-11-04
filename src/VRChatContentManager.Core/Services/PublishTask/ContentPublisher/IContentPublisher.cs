namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public interface IContentPublisher
{
    string GetContentType();
    string GetContentName();
    string GetContentPlatform();

    ValueTask PublishAsync(
        string bundleFileId,
        HttpClient awsClient,
        PublishStageProgressReporter progressReporter,
        CancellationToken cancellationToken = default
    );
}