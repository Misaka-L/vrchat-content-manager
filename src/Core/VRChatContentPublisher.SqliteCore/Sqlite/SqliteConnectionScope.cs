using Microsoft.Data.Sqlite;

namespace VRChatContentPublisher.PersistentCore.Sqlite;

public sealed class SqliteConnectionScope(SqliteConnection sqliteConnection, Action onRelease) : IDisposable
{
    public SqliteConnection Connection => sqliteConnection;

    public void Dispose()
    {
        onRelease();
    }
}