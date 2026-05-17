using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Database;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.Core.Services.Rpc.RpcFIle;

/// <summary>
/// On startup, reconciles the <c>RpcFiles</c> database table with the filesystem
/// to clean up orphaned records and files left behind by interrupted writes or crashes.
/// </summary>
public sealed class FileCleanupHostedService(
    FileDatabaseService fileDatabaseService,
    ILogger<FileCleanupHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting file database cleanup...");

        await CleanupWritingRecordsAsync(cancellationToken);
        await CleanupDanglingDbRecordsAsync(cancellationToken);
        await CleanupOrphanDiskFilesAsync(cancellationToken);

        logger.LogInformation("File database cleanup completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes files and their database records that are stuck in <c>Writing</c> status.
    /// A <c>Writing</c> record at startup means the previous process crashed
    /// or was terminated before it could mark the file as <c>Ready</c>.
    /// </summary>
    private async Task CleanupWritingRecordsAsync(CancellationToken cancellationToken)
    {
        var writingRecords = await fileDatabaseService.GetWritingFileRecordsAsync();
        if (writingRecords.Count == 0)
            return;

        logger.LogInformation("Found {Count} file record(s) stuck in Writing status — cleaning up…",
            writingRecords.Count);

        foreach (var record in writingRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (File.Exists(record.FilePath))
                {
                    File.Delete(record.FilePath);
                    logger.LogDebug("Deleted orphaned file: {FilePath}", record.FilePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete orphaned file: {FilePath}", record.FilePath);
            }

            try
            {
                await fileDatabaseService.DeleteFileRecordAsync(record.FileId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete Writing database record: {FileId}", record.FileId);
            }
        }
    }

    /// <summary>
    /// Removes database records whose corresponding files no longer exist on disk.
    /// </summary>
    private async Task CleanupDanglingDbRecordsAsync(CancellationToken cancellationToken)
    {
        var readyRecords = await fileDatabaseService.GetAllReadyFileRecordsAsync();

        var danglingCount = 0;
        foreach (var record in readyRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(record.FilePath))
                continue;

            danglingCount++;
            try
            {
                await fileDatabaseService.DeleteFileRecordAsync(record.FileId!);
                logger.LogDebug("Removed dangling database record for missing file: {FilePath} (FileId: {FileId})",
                    record.FilePath, record.FileId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete dangling database record: {FileId}", record.FileId);
            }
        }

        if (danglingCount > 0)
            logger.LogInformation("Removed {Count} dangling database record(s) pointing to missing files.",
                danglingCount);
    }

    /// <summary>
    /// Deletes files on disk under the <c>rpc-files</c> directory that have
    /// no corresponding record in the <c>RpcFiles</c> table.
    /// </summary>
    private async Task CleanupOrphanDiskFilesAsync(CancellationToken cancellationToken)
    {
        var rootPath = GetFileRootPath();
        if (!Directory.Exists(rootPath))
            return;

        var knownFileIds = await fileDatabaseService.GetAllFileIdsAsync();

        var orphanCount = 0;
        foreach (var filePath in Directory.EnumerateFiles(rootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(filePath);
            if (knownFileIds.Contains(fileName))
                continue;

            orphanCount++;
            try
            {
                File.Delete(filePath);
                logger.LogDebug("Deleted orphaned disk file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete orphaned disk file: {FilePath}", filePath);
            }
        }

        if (orphanCount > 0)
            logger.LogInformation("Deleted {Count} orphaned disk file(s) with no database record.", orphanCount);
    }

    private static string GetFileRootPath()
    {
        var storagePath = AppStorageService.GetStoragePath();
        var rootPath = Path.Combine(storagePath, "rpc-files");

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        return rootPath;
    }
}
