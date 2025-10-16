namespace VRChatContentManager.ConnectCore.Services.PublishTask;

public interface IWorldPublishTaskService
{
    ValueTask<string> CreatePublishTaskAsync(
        string worldId,
        string worldBundleFileId,
        string platform,
        string unityVersion,
        string? worldSignature
    );
}