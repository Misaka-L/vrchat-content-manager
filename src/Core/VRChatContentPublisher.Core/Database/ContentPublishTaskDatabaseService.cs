using VRChatContentPublisher.PersistentCore.Sqlite;

namespace VRChatContentPublisher.Core.Database;

public sealed class ContentPublishTaskDatabaseService(SqliteDatabaseService sqliteDatabaseService)
{
    public async Task InitializeAsync()
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            """
            CREATE TABLE IF NOT EXISTS ContentPublishTasks (
                TaskId TEXT PRIMARY KEY,
                ContentType TEXT NOT NULL,
                ContentId TEXT NOT NULL,
                ContentPlatform TEXT NOT NULL,
                Status TEXT NOT NULL,
                StateJson TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
    }
}