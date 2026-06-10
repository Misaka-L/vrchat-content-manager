using System.Diagnostics;

namespace VRChatContentPublisher.Core.Telemetry;

public static class CoreActivitySources
{
    public const string ContentPublishingActivitySourceName = "VRChatContentPublisher.Core.ContentPublishing";

    internal static readonly ActivitySource ContentPublishing = new(ContentPublishingActivitySourceName);
}