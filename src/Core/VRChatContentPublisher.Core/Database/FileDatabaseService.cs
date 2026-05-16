using Microsoft.Data.Sqlite;
using VRChatContentPublisher.PersistentCore.Sqlite;

namespace VRChatContentPublisher.Core.Database;

public sealed class FileDatabaseService(SqliteDatabaseService sqliteDatabaseService)
{
    public async Task InitializeAsync()
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            """
            CREATE TABLE IF NOT EXISTS RpcFiles (
                FileId TEXT PRIMARY KEY,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
    }

    public async Task CreateFileRecordAsync(string fileId, string fileName, string filePath)
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "INSERT INTO RpcFiles (FileId, FileName, FilePath) VALUES (@FileId, @FileName, @FilePath)",
            new SqliteParameter("@FileId", fileId),
            new SqliteParameter("@FileName", fileName),
            new SqliteParameter("@FilePath", filePath));
    }

    public async Task<FileEntry?> GetFileRecordAsync(string fileId)
    {
        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT FileName, FilePath FROM RpcFiles WHERE FileId = @FileId",
            new SqliteParameter("@FileId", fileId));

        if (await reader.ReadAsync())
        {
            return new FileEntry(
                reader.GetString(0),
                reader.GetString(1)
            );
        }

        return null;
    }

    public async ValueTask<bool> IsFileExistAsync(string fileId)
    {
        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT COUNT(1) FROM RpcFiles WHERE FileId = @FileId",
            new SqliteParameter("@FileId", fileId));

        if (await reader.ReadAsync())
        {
            return reader.GetInt32(0) > 0;
        }

        return false;
    }

    public async Task DeleteFileRecordAsync(string fileId)
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "DELETE FROM RpcFiles WHERE FileId = @FileId",
            new SqliteParameter("@FileId", fileId));
    }

    public record FileEntry(string FileName, string FilePath);
}
