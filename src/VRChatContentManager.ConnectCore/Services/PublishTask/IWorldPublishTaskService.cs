namespace VRChatContentManager.ConnectCore.Services.PublishTask;

public interface IWorldPublishTaskService
{
    ValueTask<string> CreatePublishTaskAsync(
        string worldId,
        string worldBundleFileId,
        string worldName,
        string platform,
        string unityVersion,
        string? worldSignature,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus
    );
}