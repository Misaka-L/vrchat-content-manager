using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Shared;
using VRChatContentPublisher.PersistentCore.Sqlite.Command;
using VRChatContentPublisher.PersistentCore.Telemetry;

namespace VRChatContentPublisher.PersistentCore.Sqlite;

public sealed class SqliteDatabaseService(ILogger<SqliteDatabaseService> logger) : IDisposable
{
    public enum SqliteDatabaseState
    {
        Uninitialized,
        Initializing,
        Initialized,
        Stopping,
        Stopped
    }

    public SqliteDatabaseState State = SqliteDatabaseState.Uninitialized;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private Channel<ISqliteCommand>? _channel;
    private TaskCompletionSource? _sqliteInitializedTcs;
    private Task? _sqliteWorkerTask;

    public async Task InitializeAsync(string pathToDatabase)
    {
        using var activity =
            SqliteCoreActivitySources.SqliteCore.StartActivity("InitializeDatabase", ActivityKind.Client);

        activity?.SetTag(
            SqliteCoreActivitySources.DatabaseSystemNameTag,
            SqliteCoreActivitySources.DatabaseSystemNameTag
        );

        using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
        {
            switch (State)
            {
                case SqliteDatabaseState.Initializing:
                    throw new InvalidOperationException("Database service is initializing.");
                case SqliteDatabaseState.Initialized:
                    throw new InvalidOperationException("Database service has already been initialized.");
                case SqliteDatabaseState.Stopping:
                    throw new InvalidOperationException("Database service is stopping.");
            }

            try
            {
                State = SqliteDatabaseState.Initializing;
                _channel = Channel.CreateBounded<ISqliteCommand>(new BoundedChannelOptions(1)
                {
                    AllowSynchronousContinuations = false, // `true` will cause a deadlock.
                    SingleWriter = true,
                    SingleReader = true
                });
                _sqliteInitializedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _sqliteWorkerTask = Task.Factory.StartNew(
                    () => SqliteWorkerLoop(pathToDatabase, _channel, _sqliteInitializedTcs),
                    TaskCreationOptions.LongRunning).Unwrap();

                await _sqliteInitializedTcs.Task;
                State = SqliteDatabaseState.Initialized;
            }
            catch (OperationCanceledException)
            {
                State = SqliteDatabaseState.Stopped;
            }
            catch (Exception)
            {
                State = SqliteDatabaseState.Stopped;
                throw;
            }
        }
    }

    private static async Task SqliteWorkerLoop(
        string pathToDatabase, Channel<ISqliteCommand> channel,
        TaskCompletionSource sqliteInitializedTcs)
    {
        SqliteConnection? connection;

        try
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = pathToDatabase,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true
            };

            connection = new SqliteConnection(connectionStringBuilder.ToString());
            await connection.OpenAsync();

            // Enable WAL (Write-Ahead Logging): https://www.sqlite.org/wal.html
            // https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async
            await using var walCommand = connection.CreateCommand();
            walCommand.CommandText = "PRAGMA journal_mode = WAL;";
            await walCommand.ExecuteNonQueryAsync();
            sqliteInitializedTcs.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            sqliteInitializedTcs.TrySetCanceled();
            return;
        }
        catch (Exception ex)
        {
            sqliteInitializedTcs.TrySetException(ex);
            return;
        }

        try
        {
            await foreach (var sqliteCommand in channel.Reader.ReadAllAsync(CancellationToken.None))
            {
                sqliteCommand.ExecuteCommand(connection);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public Task<T> ExecuteReaderAsync<T>(string commandText, Func<SqliteDataReader, T> consumer) where T : class
    {
        return ExecuteReaderAsync(commandText, [], consumer);
    }

    public async Task<T> ExecuteReaderAsync<T>(string commandText, SqliteParameter[] parameters,
        Func<SqliteDataReader, T> consumer)
    {
        using var activity = SqliteCoreActivitySources.SqliteCore.StartActivity(commandText, ActivityKind.Client);
        using (logger.BeginScope("Executing SQL command: {CommandText}", commandText))
        {
            activity?.SetTag(
                SqliteCoreActivitySources.DatabaseSystemNameTag,
                SqliteCoreActivitySources.DatabaseSystemNameTag
            );
            activity?.SetTag("db.query.summary", commandText);

            logger.LogInformation("Executing SQL command: {CommandText}", commandText);
            try
            {
                using var waitActivity =
                    SqliteCoreActivitySources.SqliteCore.StartActivity("WaitForSemaphore", ActivityKind.Client);
                using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
                {
                    waitActivity?.Stop();
                    ThrowOnInvalidState();

                    var command = new SqliteQueryCommand<T>(consumer, commandText, parameters);
                    await WaitUntilWorkerStoppedAsync(_channel.Writer.WriteAsync(command).AsTask());
                    return await WaitUntilWorkerStoppedAsync(command.ExecuteTask);
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                if (ex is SqliteException sqliteEx)
                {
                    activity?.SetTag("error.type", sqliteEx.Message);
                    activity?.SetTag("db.response.status_code",
                        sqliteEx.SqliteErrorCode + "/" + sqliteEx.SqliteExtendedErrorCode);
                }
                else
                {
                    activity?.SetTag("error.type", ex.GetType().Name);
                }

                logger.LogError(ex, "An error occurred while executing SQL command: {CommandText}", commandText);
                throw;
            }
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string commandText, params SqliteParameter[] parameters)
    {
        using var activity = SqliteCoreActivitySources.SqliteCore.StartActivity(commandText, ActivityKind.Client);
        using (logger.BeginScope("Executing (Non-Query) SQL command: {CommandText}", commandText))
        {
            activity?.SetTag(
                SqliteCoreActivitySources.DatabaseSystemNameTag,
                SqliteCoreActivitySources.DatabaseSystemNameTag
            );
            activity?.SetTag("db.query.summary", commandText);

            logger.LogInformation("Executing (Non-Query) SQL command: {CommandText}", commandText);
            try
            {
                using var waitActivity =
                    SqliteCoreActivitySources.SqliteCore.StartActivity("WaitForSemaphore", ActivityKind.Client);
                using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
                {
                    waitActivity?.Stop();
                    ThrowOnInvalidState();

                    var command = new SqliteNonQueryCommand(commandText, parameters);
                    await WaitUntilWorkerStoppedAsync(_channel.Writer.WriteAsync(command).AsTask());
                    return await WaitUntilWorkerStoppedAsync(command.ExecuteTask);
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                if (ex is SqliteException sqliteEx)
                {
                    activity?.SetTag("error.type", sqliteEx.Message);
                    activity?.SetTag("db.response.status_code",
                        sqliteEx.SqliteErrorCode + "/" + sqliteEx.SqliteExtendedErrorCode);
                }
                else
                {
                    activity?.SetTag("error.type", ex.GetType().Name);
                }

                logger.LogError(
                    ex, "An error occurred while executing (Non-Query) SQL command: {CommandText}", commandText);
                throw;
            }
        }
    }

    public async Task ShutdownAsync()
    {
        using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
        {
            switch (State)
            {
                case SqliteDatabaseState.Uninitialized:
                    logger.LogWarning("Attempted to shutdown the database connection, but State is Uninitialized.");
                    return;
                case SqliteDatabaseState.Stopping:
                    logger.LogWarning("Attempted to shutdown the database connection, but State is Stopping.");
                    return;
                case SqliteDatabaseState.Stopped:
                    logger.LogWarning("Attempted to shutdown the database connection, but State is Stopped.");
                    return;
            }

            try
            {
                State = SqliteDatabaseState.Stopping;

                _channel?.Writer.TryComplete();

                if (_sqliteWorkerTask != null)
                {
                    await _sqliteWorkerTask;
                }

                _channel = null;
                _sqliteInitializedTcs = null;
                _sqliteWorkerTask = null;

                State = SqliteDatabaseState.Stopped;
                logger.LogInformation("Database service stopped.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while shutting down the database connection.");
            }
        }
    }

    [MemberNotNull(nameof(_channel), nameof(_sqliteWorkerTask))]
    private void ThrowOnInvalidState()
    {
        if (State != SqliteDatabaseState.Initialized)
            throw new InvalidOperationException($"Database service not initialized, State: {State}");
        if (_channel == null)
            throw new InvalidOperationException($"Database service not initialized: channel is null.");
        if (_sqliteWorkerTask == null)
            throw new InvalidOperationException($"Database service not initialized: worker task is null.");
    }

    private async Task WaitUntilWorkerStoppedAsync(Task task)
    {
        ThrowOnInvalidState();
        await Task.WhenAny(task, _sqliteWorkerTask);
        if (task.IsCompleted)
        {
            await task;
        }
        else
        {
            State = SqliteDatabaseState.Stopped;
            await _sqliteWorkerTask;
            throw new InvalidOperationException($"Database service stopped, worker status: {_sqliteWorkerTask.Status}");
        }
    }

    private async Task<T> WaitUntilWorkerStoppedAsync<T>(Task<T> task)
    {
        ThrowOnInvalidState();
        await Task.WhenAny(task, _sqliteWorkerTask);
        if (task.IsCompleted)
        {
            return await task;
        }
        else
        {
            State = SqliteDatabaseState.Stopped;
            await _sqliteWorkerTask;
            throw new InvalidOperationException($"Database service stopped, worker status: {_sqliteWorkerTask.Status}");
        }
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }
}