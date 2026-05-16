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
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Status TEXT NOT NULL
            );
            """);
    }

    public async Task CreateFileRecordAsync(string fileId, string fileName, string filePath,
        string status = "Ready")
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "INSERT INTO RpcFiles (FileId, FileName, FilePath, Status) VALUES (@FileId, @FileName, @FilePath, @Status)",
            new SqliteParameter("@FileId", fileId),
            new SqliteParameter("@FileName", fileName),
            new SqliteParameter("@FilePath", filePath),
            new SqliteParameter("@Status", status));
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

    public async Task MarkFileReadyAsync(string fileId)
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "UPDATE RpcFiles SET Status = 'Ready' WHERE FileId = @FileId",
            new SqliteParameter("@FileId", fileId));
    }

    public async Task DeleteFileRecordAsync(string fileId)
    {
        await sqliteDatabaseService.ExecuteNonQueryAsync(
            "DELETE FROM RpcFiles WHERE FileId = @FileId",
            new SqliteParameter("@FileId", fileId));
    }

    public async Task<IReadOnlyList<WritingFileEntry>> GetWritingFileRecordsAsync()
    {
        var records = new List<WritingFileEntry>();

        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT FileId, FilePath FROM RpcFiles WHERE Status = 'Writing'");

        while (await reader.ReadAsync())
        {
            records.Add(new WritingFileEntry(
                reader.GetString(0),
                reader.GetString(1)
            ));
        }

        return records;
    }

    public async Task<IReadOnlyList<FileEntry>> GetAllReadyFileRecordsAsync()
    {
        var records = new List<FileEntry>();

        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT FileId, FileName, FilePath FROM RpcFiles WHERE Status = 'Ready'");

        while (await reader.ReadAsync())
        {
            records.Add(new FileEntry(
                reader.GetString(1),
                reader.GetString(2)
            )
            { FileId = reader.GetString(0) });
        }

        return records;
    }

    public async Task<IReadOnlySet<string>> GetAllFileIdsAsync()
    {
        var fileIds = new HashSet<string>();

        await using var reader = await sqliteDatabaseService.ExecuteReaderAsync(
            "SELECT FileId FROM RpcFiles");

        while (await reader.ReadAsync())
        {
            fileIds.Add(reader.GetString(0));
        }

        return fileIds;
    }

    public record FileEntry(string FileName, string FilePath)
    {
        public string? FileId { get; init; }
    }

    public record WritingFileEntry(string FileId, string FilePath);
}
