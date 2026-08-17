using Microsoft.Data.Sqlite;

namespace VRChatContentPublisher.PersistentCore.Sqlite.Command;

public sealed class SqliteNonQueryCommand(string commandText, params SqliteParameter[] parameters)
    : ISqliteCommand
{
    private readonly TaskCompletionSource<int> _tcs = new();

    public Task<int> ExecuteTask => _tcs.Task;

    public void ExecuteCommand(SqliteConnection connection)
    {
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Parameters.AddRange(parameters);
            _tcs.TrySetResult(command.ExecuteNonQuery());
        }
        catch (Exception ex)
        {
            _tcs.TrySetException(ex);
        }
    }
}