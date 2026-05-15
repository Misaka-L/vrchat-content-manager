using System.Collections.Concurrent;
using VRChatContentPublisher.ConnectCore.Models;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.Core.Services;

public sealed class RpcFileStorageService : IFileService
{
    private readonly ConcurrentDictionary<string, FileEntry> _fileMap = [];

    public ValueTask<UploadFileTask> GetUploadFileStreamAsync(string fileName)
    {
        var rootPath = GetFileRootPath();

        var fileId = Guid.NewGuid().ToString("N");
        var filePath = Path.Combine(rootPath, fileId);

        var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096,
            FileOptions.Asynchronous);
        var uploadFileTask = new UploadFileTask(fileStream, fileId);

        _fileMap[fileId] = new FileEntry(fileName, filePath);

        return ValueTask.FromResult(uploadFileTask);
    }

    public ValueTask<Stream?> GetFileAsync(string fileId)
    {
        if (!_fileMap.TryGetValue(fileId, out var fileEntry))
            return ValueTask.FromResult<Stream?>(null);

        return ValueTask.FromResult<Stream?>(File.OpenRead(fileEntry.FilePath));
    }

    public ValueTask<UploadedFile?> GetFileWithNameAsync(string fileId)
    {
        if (!_fileMap.TryGetValue(fileId, out var fileEntry))
            return ValueTask.FromResult<UploadedFile?>(null);

        var fileName = Path.GetFileName(fileEntry.FileName);
        var fileStream = File.OpenRead(fileEntry.FilePath);

        var uploadedFile = new UploadedFile
        {
            FileName = fileName,
            FileStream = fileStream
        };

        return ValueTask.FromResult<UploadedFile?>(uploadedFile);
    }

    public ValueTask<bool> IsFileExistAsync(string fileId)
    {
        return ValueTask.FromResult(_fileMap.ContainsKey(fileId));
    }

    public ValueTask DeleteFileAsync(string fileId)
    {
        if (_fileMap.TryRemove(fileId, out var fileEntry))
        {
            if (File.Exists(fileEntry.FilePath))
            {
                File.Delete(fileEntry.FilePath);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static string GetFileRootPath()
    {
        var storagePath = AppStorageService.GetStoragePath();
        var rootPath = Path.Combine(storagePath, "rpc-files");

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        return rootPath;
    }

    private record FileEntry(string FileName, string FilePath);
}
