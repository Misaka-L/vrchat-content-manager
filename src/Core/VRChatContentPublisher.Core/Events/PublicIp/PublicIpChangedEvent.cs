namespace VRChatContentPublisher.Core.Events.PublicIp;

public sealed class PublicIpChangedEvent(
    string oldIpPlaintext,
    string newIpPlaintext,
    string oldIpEncrypted,
    string newIpEncrypted,
    DateTimeOffset detectedAtUtc)
{
    public string OldIpPlaintext => oldIpPlaintext;
    public string NewIpPlaintext => newIpPlaintext;

    public string OldIpEncrypted => oldIpEncrypted;
    public string NewIpEncrypted => newIpEncrypted;

    public DateTimeOffset DetectedAtUtc => detectedAtUtc;
}