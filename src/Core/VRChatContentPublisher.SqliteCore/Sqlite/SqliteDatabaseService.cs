using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Shared;
using VRChatContentPublisher.PersistentCore.Telemetry;

namespace VRChatContentPublisher.PersistentCore.Sqlite;

public sealed class SqliteDatabaseService(ILogger<SqliteDatabaseService> logger) : IAsyncDisposable
{
    private SqliteConnection? _connection;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public async Task InitializeAsync(string pathToDatabase)
    {
        using (var activity =
               SqliteCoreActivitySources.SqliteCore.StartActivity("InitializeDatabase", ActivityKind.Client))
        using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
        {
            activity?.SetTag(
                SqliteCoreActivitySources.DatabaseSystemNameTag,
                SqliteCoreActivitySources.DatabaseSystemNameTag
            );

            if (_connection is not null)
                throw new InvalidOperationException("Database service has already been initialized.");

            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = pathToDatabase,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true
            };

            var connectionString = connectionStringBuilder.ToString();
            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync();

            // Enable WAL (Write-Ahead Logging): https://www.sqlite.org/wal.html
            // https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async
            await using var walCommand = _connection.CreateCommand();
            walCommand.CommandText = "PRAGMA journal_mode = WAL;";
            await walCommand.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask<SqliteDataReader> ExecuteReaderAsync(string commandText, params SqliteParameter[] parameters)
    {
        using (var activity = SqliteCoreActivitySources.SqliteCore.StartActivity(commandText, ActivityKind.Client))
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
                using (var waitActivity =
                       SqliteCoreActivitySources.SqliteCore.StartActivity("WaitForSemaphore", ActivityKind.Client))
                using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
                {
                    waitActivity?.Stop();
                    ThrowOnInvalidState();

                    var command = _connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Parameters.AddRange(parameters);
                    return await command.ExecuteReaderAsync();
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

    public async ValueTask<int> ExecuteNonQueryAsync(string commandText, params SqliteParameter[] parameters)
    {
        using (var activity = SqliteCoreActivitySources.SqliteCore.StartActivity(commandText, ActivityKind.Client))
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
                using (var waitActivity =
                       SqliteCoreActivitySources.SqliteCore.StartActivity("WaitForSemaphore", ActivityKind.Client))
                using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
                {
                    waitActivity?.Stop();
                    ThrowOnInvalidState();

                    await using var command = _connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Parameters.AddRange(parameters);
                    return await command.ExecuteNonQueryAsync();
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

    public async ValueTask<SqliteConnectionScope> GetConnectionScopeAsync()
    {
        ThrowOnInvalidState();

        await _semaphoreSlim.WaitAsync();
        return new SqliteConnectionScope(_connection, () => _semaphoreSlim.Release());
    }

    public async Task ShutdownAsync()
    {
        try
        {
            using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
            {
                if (_connection is null)
                {
                    logger.LogWarning("Attempted to shut down the database connection, but it was not initialized.");
                    return;
                }

                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while shutting down the database connection.");
        }
    }

    [MemberNotNull(nameof(_connection))]
    private void ThrowOnInvalidState()
    {
        if (_connection is null)
            throw new InvalidOperationException("Database service is not initialized.");
        if (_connection.State == ConnectionState.Closed)
            throw new InvalidOperationException("Database connection was closed.");
        if (_connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Database connection is in an invalid state: " + _connection.State);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
        _semaphoreSlim.Dispose();
    }
}