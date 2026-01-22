using VRChatContentPublisher.ConnectCore.Models;

namespace VRChatContentPublisher.ConnectCore.Services;

public interface IFileService
{
    ValueTask<UploadFileTask> GetUploadFileStreamAsync(string fileName);

    ValueTask<Stream?> GetFileAsync(string fileId);

    ValueTask<UploadedFile?> GetFileWithNameAsync(string fileId);

    ValueTask<bool> IsFileExistAsync(string fileId);

    ValueTask DeleteFileAsync(string fileId);
}