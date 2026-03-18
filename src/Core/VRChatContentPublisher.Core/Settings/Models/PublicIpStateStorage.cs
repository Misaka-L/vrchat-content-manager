namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class PublicIpStateStorage
{
    public string? LastPublicIp { get; set; }
    public DateTimeOffset? LastChangedAtUtc { get; set; }
    public DateTimeOffset? LastCheckedAtUtc { get; set; }
    public Guid? LastWarningInstanceId { get; set; }
}