using Microsoft.Data.Sqlite;

namespace VRChatContentPublisher.PersistentCore.Sqlite.Command;

public interface ISqliteCommand
{
    public void ExecuteCommand(SqliteConnection connection);
}