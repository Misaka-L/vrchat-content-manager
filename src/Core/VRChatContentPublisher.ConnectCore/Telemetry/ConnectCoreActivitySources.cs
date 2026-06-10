using System.Diagnostics;

namespace VRChatContentPublisher.ConnectCore.Telemetry;

public static class ConnectCoreActivitySources
{
    public const string ConnectCoreSourceName = "VRChatContentPublisher.ConnectCore";

    internal static readonly ActivitySource ConnectCoreActivitySource = new(ConnectCoreSourceName);
}