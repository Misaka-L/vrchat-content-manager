using System.Diagnostics;

namespace VRChatContentPublisher.BundleProcessCore.Telemetry;

public static class BundleProcessCoreActivitySources
{
    public const string BundleProcessCoreActivitySourceName = "VRChatContentPublisher.BundleProcessCore";

    internal static readonly ActivitySource BundleProcessCoreActivitySource = new(BundleProcessCoreActivitySourceName);
}