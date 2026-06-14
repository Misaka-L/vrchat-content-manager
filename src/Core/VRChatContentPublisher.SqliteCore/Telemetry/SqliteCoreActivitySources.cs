using System.Diagnostics;

namespace VRChatContentPublisher.PersistentCore.Telemetry;

public static class SqliteCoreActivitySources
{
    public const string SqliteCoreActivitySourceName = "VRChatContentPublisher.SqliteCore";

    internal static readonly ActivitySource SqliteCore = new(SqliteCoreActivitySourceName);

    public const string DatabaseSystemNameTag = "db.system.name";
    public const string DatabaseSystemNameTagValue = "sqlite";
}