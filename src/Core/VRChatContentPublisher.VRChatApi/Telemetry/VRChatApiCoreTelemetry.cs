using System.Diagnostics;

namespace VRChatContentPublisher.VRChatApi.Telemetry;

public static class VRChatApiCoreTelemetry
{
    public const string VRChatApiActivitySourceName = "VRChatContentPublisher.VRChatApi";

    internal static readonly ActivitySource VRChatApi = new(VRChatApiActivitySourceName);
}