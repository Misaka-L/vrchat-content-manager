using Microsoft.Data.Sqlite;

namespace VRChatContentPublisher.PersistentCore.Sqlite.Command;

public sealed class SqliteQueryCommand<T>(
    Func<SqliteDataReader, T> consumer,
    string commandText,
    params SqliteParameter[] parameters)
    : ISqliteCommand
{
    private readonly TaskCompletionSource<T> _tcs = new();

    public Task<T> ExecuteTask => _tcs.Task;

    public void ExecuteCommand(SqliteConnection connection)
    {
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Parameters.AddRange(parameters);
            using var reader = command.ExecuteReader();
            _tcs.TrySetResult(consumer(reader));
        }
        catch (Exception ex)
        {
            _tcs.TrySetException(ex);
        }
    }
}