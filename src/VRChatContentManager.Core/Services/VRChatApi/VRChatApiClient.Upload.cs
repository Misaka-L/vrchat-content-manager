using Microsoft.Extensions.Logging;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Files;
using VRChatContentManager.Core.Utils;

namespace VRChatContentManager.Core.Services.VRChatApi;

public partial class VRChatApiClient
{
    public async ValueTask<VRChatApiFileVersion> CreateAndUploadFileVersionAsync(Stream fileStream, string fileId,
        HttpClient awsClient)
    {
        var currentAssetFile = await GetFileAsync(fileId);

        // Step 1. Cleanup any incomplete file versions.
        await VRChatApiFlieUtils.CleanupIncompleteFileVersionsAsync(currentAssetFile, this);

        // Step 2.Caulate bundle file md5 and check is same file exists.
        var fileMd5 = await VRChatApiFlieUtils.GetMd5FromStreamForVRChatAsync(fileStream);
        var fileLength = fileStream.Length;

        var existingFileVersion = currentAssetFile.Versions
            .Where(version => version.File is { } file && file.Md5 == fileMd5)
            .ToArray();

        if (existingFileVersion.Length > 0)
        {
            logger.LogInformation("File with same MD5 already exists for file {FileId}", fileId);
            if (existingFileVersion.FirstOrDefault(version => version.Status == "complete") is { } completeVersion)
            {
                logger.LogInformation("Existing file version {Version} is already complete for file {FileId}",
                    completeVersion.Version, fileId);
                // TODO: What will happen? I don't know.
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        // Step 3. Caulate file signature
        var signatureStream =
            new MemoryStream(await VRChatApiFlieUtils.GetSignatureFromStreamForVRChatAsync(fileStream));
        var signatureMd5 = await VRChatApiFlieUtils.GetMd5FromStreamForVRChatAsync(signatureStream);
        var signatureLength = signatureStream.Length;

        logger.LogInformation("Creating new file version for file {FileId}", fileId);
        // Step 4. Create new file version
        var fileVersion =
            await CreateFileVersionAsync(fileId, fileMd5, fileLength, signatureMd5, signatureLength);
        
        if (fileVersion.File is null || fileVersion.Signature is null)
            throw new UnexpectedApiBehaviourException("Api did not return file or signature info for created file version.");

        // Step 5. Upload bundle file and signature to aws s3

        logger.LogInformation("Uploading file version {Version} for file {FileId}", fileVersion.Version, fileId);
        await UploadFileVersionAsync(fileStream, fileId, fileVersion.Version, fileMd5,
            fileVersion.File.Category == "simple", VRChatApiFileType.File, awsClient);

        logger.LogInformation("Uploading signature for world {FileId}", fileId);
        await UploadFileVersionAsync(signatureStream, fileId, fileVersion.Version, signatureMd5,
            fileVersion.Signature.Category == "simple", VRChatApiFileType.Signature, awsClient);

        // Step 6. Wait for server to process the uploaded file version
        logger.LogInformation("Waiting for server processing of file version {Version} for file {FileId}",
            fileVersion.Version, fileId);
        await Task.Delay(TimeSpan.FromSeconds(3));

        var completedFile = await GetFileAsync(fileId);
        return completedFile.Versions.FirstOrDefault(ver => ver.Version == fileVersion.Version) ??
               throw new UnexpectedApiBehaviourException(
                   "Api did not return the created file version after upload complete.");
    }

    private async ValueTask UploadFileVersionAsync(Stream fileStream, string fileId, int version, string md5,
        bool isSimpleUpload, VRChatApiFileType fileType, HttpClient awsClient)
    {
        if (isSimpleUpload)
        {
            var simpleUploadUrl = await GetSimpleUploadUrlAsync(fileId, version, fileType);
            await UploadFileToS3Async(simpleUploadUrl, fileStream, awsClient, md5, isSimpleUpload);
            await CompleteSimpleFileUploadAsync(fileId, version, fileType);

            return;
        }

        var uploader = concurrentMultipartUploaderFactory.Create(fileStream, fileId, version, fileType, this, awsClient);
        var eTags = await uploader.UploadAsync();
        // var firstPartUploadUrl = await GetFilePartUploadUrlAsync(fileId, version, 1, fileType);
        // var eTag = await UploadFileToS3Async(firstPartUploadUrl, fileStream, awsClient, md5, isSimpleUpload);
        await CompleteFilePartUploadAsync(fileId, version, eTags, fileType);
    }

    private async ValueTask<string> UploadFileToS3Async(string uploadUrl, Stream stream, HttpClient awsClient,
        string? md5 = null, bool isSimpleUpload = false)
    {
        var content = new StreamContent(stream);
        if (isSimpleUpload)
        {
            if (md5 is null)
                throw new ArgumentNullException(nameof(md5), "MD5 should be provided for simple upload.");
            
            content.Headers.ContentMD5 = Convert.FromBase64String(md5);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };

        var response = await awsClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        if (response.Headers.ETag is null)
            throw new UnexpectedApiBehaviourException("Api did not return an ETag header.");

        return response.Headers.ETag.Tag;
    }
}