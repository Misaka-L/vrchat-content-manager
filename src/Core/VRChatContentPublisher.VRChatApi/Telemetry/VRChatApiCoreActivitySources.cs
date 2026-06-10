using System.Diagnostics;

namespace VRChatContentPublisher.VRChatApi.Telemetry;

public static class VRChatApiCoreActivitySources
{
    public const string VRChatApiActivitySourceName = "VRChatContentPublisher.VRChatApi";

    internal static readonly ActivitySource VRChatApi = new(VRChatApiActivitySourceName);
}