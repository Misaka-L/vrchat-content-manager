namespace VRChatContentManager.ConnectCore.Models.Api.V1.Requests.Task;

public sealed class CreateWorldPublishTaskRequest
{
    public required string WorldId { get; set; }
    public required string Name { get; set; }
    public required string WorldBundleFileId { get; set; }
    public required string Platform { get; set; }
    public required string UnityVersion { get; set; }
    public string? WorldSignature { get; set; }
}