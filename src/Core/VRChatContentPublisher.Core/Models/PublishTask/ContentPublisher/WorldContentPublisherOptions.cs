namespace VRChatContentPublisher.Core.Models.PublishTask.ContentPublisher;

/// <summary>
/// Serializable creation parameters for <see cref="VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher.WorldContentPublisher"/>.
/// All fields are pure data — no runtime service references.
/// </summary>
public sealed class WorldContentPublisherOptions
{
    public string WorldId { get; set; } = string.Empty;
    public string WorldName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
    public string? WorldSignature { get; set; }
    public int? Capacity { get; set; }
    public int? RecommendedCapacity { get; set; }
    public string? PreviewYoutubeId { get; set; }
    public string[]? UdonProducts { get; set; }
}
