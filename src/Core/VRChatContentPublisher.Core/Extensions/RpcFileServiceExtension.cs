using VRChatContentPublisher.ConnectCore.Models;
using VRChatContentPublisher.ConnectCore.Services;

namespace VRChatContentPublisher.Core.Extensions;

public static class RpcFileServiceExtension
{
    public static async ValueTask<
        (Stream bundleFileStream, UploadedFile? thumbnailFile)
    > GetRpcFileStream(this IFileService fileService, string bundleFileId, string? thumbnailFileId)
    {
        Stream? bundleFileStream = null;
        Stream? thumbnailFileStream = null;
        try
        {
            bundleFileStream = await fileService.GetFileAsync(bundleFileId);
            var thumbnailFile = thumbnailFileId is not null
                ? await fileService.GetFileWithNameAsync(thumbnailFileId)
                : null;
            thumbnailFileStream = thumbnailFile?.FileStream;

            if (bundleFileStream is null)
                throw new ArgumentException("Could not find the provided bundle file.", nameof(bundleFileId));

            if (thumbnailFile is null && thumbnailFileId is not null)
                throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));
            return (bundleFileStream, thumbnailFile);
        }
        catch
        {
            if (thumbnailFileStream != null) await thumbnailFileStream.DisposeAsync();
            if (bundleFileStream != null) await bundleFileStream.DisposeAsync();
            throw;
        }
    }
}