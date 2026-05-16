namespace VRChatContentPublisher.Core.Models;

/// <summary>
/// Serializable creation parameters for <see cref="ContentPublisher.AvatarContentPublisher"/>.
/// All fields are pure data — no runtime service references.
/// </summary>
public sealed class AvatarContentPublisherOptions
{
    public string AvatarId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
}
