namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class PublicIpStateStorage
{
    public string? LastPreviousIp { get; set; }
    public string? LastPublicIp { get; set; }
    public DateTimeOffset? LastChangedAtUtc { get; set; }
    public DateTimeOffset? LastCheckedAtUtc { get; set; }
    public bool IsWarningDismissed { get; set; } = true;
}