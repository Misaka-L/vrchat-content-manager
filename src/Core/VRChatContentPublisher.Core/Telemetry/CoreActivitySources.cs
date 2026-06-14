using System.Diagnostics;

namespace VRChatContentPublisher.Core.Telemetry;

public static class CoreActivitySources
{
    public const string ContentPublishingActivitySourceName = "VRChatContentPublisher.Core.ContentPublishing";
    public const string RpcActivitySourceName = "VRChatContentPublisher.Core.Rpc";

    internal static readonly ActivitySource ContentPublishing = new(ContentPublishingActivitySourceName);
    internal static readonly ActivitySource Rpc = new(RpcActivitySourceName);
}