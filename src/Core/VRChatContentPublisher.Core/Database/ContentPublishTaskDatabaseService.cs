using System.Text.Json;
using Microsoft.Data.Sqlite;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Models.PublishTask;
using VRChatContentPublisher.PersistentCore.Sqlite;
using ContentPublishTaskStateJsonContext = VRChatContentPublisher.Core.Models.PublishTask.ContentPublishTaskStateJsonContext;

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
                StateJson TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
    }

    #region Task State CRUD

    public async Task SaveStateAsync(ContentPublishTaskState state)
    {
        var stateJson = JsonSerializer.Serialize(state, ContentPublishTaskStateJsonContext.Default.ContentPublishTaskState);

        await sqliteDatabaseService.ExecuteNonQueryAsync(
            """
            INSERT OR REPLACE INTO ContentPublishTasks
                (TaskId, ContentType, ContentId, ContentPlatform, StateJson, CreatedAt)
            VALUES
                (@TaskId, @ContentType, @ContentId, @ContentPlatform, @StateJson, @CreatedAt)
            """,
            new SqliteParameter("@TaskId", state.TaskId),
            new SqliteParameter("@ContentType", state.ContentType),
            new SqliteParameter("@ContentId", state.ContentId),
            new SqliteParameter("@ContentPlatform", state.ContentPlatform),
            new SqliteParameter("@StateJson", stateJson),
            new SqliteParameter("@CreatedAt", state.CreatedTime.ToString("O")));
    }

    public async Task<ContentPublishTaskState?> GetStateAsync(string taskId)
    {
        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT StateJson FROM ContentPublishTasks WHERE TaskId = @TaskId",
            new SqliteParameter("@TaskId", taskId));

        if (await reader.ReadAsync())
        {
            var stateJson = reader.GetString(0);
            return JsonSerializer.Deserialize(stateJson, ContentPublishTaskStateJsonContext.Default.ContentPublishTaskState);
        }

        return null;
    }

    public async Task<IReadOnlyList<ContentPublishTaskState>> GetAllStatesAsync()
    {
        var states = new List<ContentPublishTaskState>();

        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT StateJson FROM ContentPublishTasks ORDER BY CreatedAt");

        while (await reader.ReadAsync())
        {
            var stateJson = reader.GetString(0);
            var state = JsonSerializer.Deserialize(stateJson, ContentPublishTaskStateJsonContext.Default.ContentPublishTaskState);
            if (state is not null)
                states.Add(state);
        }

        return states;
    }

    public async Task DeleteStateAsync(string taskId)
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "DELETE FROM ContentPublishTasks WHERE TaskId = @TaskId",
            new SqliteParameter("@TaskId", taskId));
    }

    #endregion
}