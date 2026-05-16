using VRChatContentPublisher.ConnectCore.Models;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.Database;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.Core.Services;

public sealed class RpcFileStorageService : IFileService
{
    private readonly FileDatabaseService _fileDatabaseService;

    public RpcFileStorageService(FileDatabaseService fileDatabaseService)
    {
        _fileDatabaseService = fileDatabaseService;
    }

    public async ValueTask<UploadFileTask> GetUploadFileStreamAsync(string fileName)
    {
        var rootPath = GetFileRootPath();

        var fileId = Guid.NewGuid().ToString("N");
        var filePath = Path.Combine(rootPath, fileId);

        var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096,
            FileOptions.Asynchronous);

        await _fileDatabaseService.CreateFileRecordAsync(fileId, fileName, filePath, status: "Writing");

        return new UploadFileTask(fileStream, fileId);
    }

    public async ValueTask<Stream?> GetFileAsync(string fileId)
    {
        var fileEntry = await _fileDatabaseService.GetFileRecordAsync(fileId);
        if (fileEntry is null)
            return null;

        if (!File.Exists(fileEntry.FilePath))
            return null;

        return File.OpenRead(fileEntry.FilePath);
    }

    public async ValueTask<UploadedFile?> GetFileWithNameAsync(string fileId)
    {
        var fileEntry = await _fileDatabaseService.GetFileRecordAsync(fileId);
        if (fileEntry is null)
            return null;

        if (!File.Exists(fileEntry.FilePath))
            return null;

        var fileName = Path.GetFileName(fileEntry.FileName);
        var fileStream = File.OpenRead(fileEntry.FilePath);

        return new UploadedFile
        {
            FileName = fileName,
            FileStream = fileStream
        };
    }

    public ValueTask<bool> IsFileExistAsync(string fileId)
    {
        return _fileDatabaseService.IsFileExistAsync(fileId);
    }

    public async ValueTask MarkFileReadyAsync(string fileId)
    {
        await _fileDatabaseService.MarkFileReadyAsync(fileId);
    }

    public async ValueTask DeleteFileAsync(string fileId)
    {
        var fileEntry = await _fileDatabaseService.GetFileRecordAsync(fileId);
        if (fileEntry is not null)
        {
            if (File.Exists(fileEntry.FilePath))
            {
                File.Delete(fileEntry.FilePath);
            }
        }

        await _fileDatabaseService.DeleteFileRecordAsync(fileId);
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
