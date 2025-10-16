namespace VRChatContentManager.ConnectCore.Models.Api.V1.Requests.Task;

public sealed class CreateAvatarPublishTaskRequest
{
    public required string AvatarId { get; set; }
    public required string Name { get; set; }
    public required string AvatarBundleFileId { get; set; }
    public required string Platform { get; set; }
    public required string UnityVersion { get; set; }
}