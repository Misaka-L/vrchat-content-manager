namespace VRChatContentPublisher.Core.Events.PublicIp;

public sealed class PublicIpChangedEvent(
    string oldIpPlaintext,
    string newIpPlaintext,
    DateTimeOffset detectedAtUtc) : EventArgs
{
    public string OldIpPlaintext => oldIpPlaintext;
    public string NewIpPlaintext => newIpPlaintext;

    public DateTimeOffset DetectedAtUtc => detectedAtUtc;
}