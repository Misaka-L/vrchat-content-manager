namespace VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;

public interface IContentPublisher
{
    string GetContentType();
    string GetContentName();
    string GetContentPlatform();

    bool CanPublish();

    ValueTask BeforePublishTaskAsync(
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        CancellationToken cancellationToken = default
    );

    ValueTask PublishAsync(
        string bundleFileId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        PublishStageProgressReporter progressReporter,
        CancellationToken cancellationToken = default
    );
}