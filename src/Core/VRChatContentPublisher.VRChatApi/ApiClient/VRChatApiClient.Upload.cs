using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.VRChatApi.Exceptions;
using VRChatContentPublisher.VRChatApi.Models.ProgressReport;
using VRChatContentPublisher.VRChatApi.Models.Rest.Files;
using VRChatContentPublisher.VRChatApi.Models.Rest.UnityPackages;
using VRChatContentPublisher.VRChatApi.Utils;

namespace VRChatContentPublisher.VRChatApi.ApiClient;

public partial class VRChatApiClient
{
    public static async ValueTask<string> GetOrCreateBundleFileIdAsync(
        VRChatApiClient apiClient, VRChatApiUnityPackage[] unityPackages, string fileName, string platform)
    {
        var platformApiUnityPackage = VRChatApiFileUtils.TryGetUnityPackageForPlatform(unityPackages, platform);
        if (platformApiUnityPackage is not null)
        {
            var fileId = VRChatApiFileUtils.TryGetFileIdFromAssetUrl(platformApiUnityPackage.AssetUrl);
            if (fileId is null)
                throw new UnexpectedApiBehaviourException("Api returned an invalid asset url.");

            return fileId;
        }

        var extension = Path.GetExtension(fileName);
        var file = await apiClient.CreateFileAsync(fileName, VRChatApiFileUtils.GetMimeTypeFromExtension(extension),
            extension);
        return file.Id;
    }

    public async ValueTask<string> UploadThumbnailAsync(
        Stream thumbnailStream, string contentType, string thumbnailFileName, string? previousImageUrl = null,
        Action<PublishTaskProgressEventArg>? progressCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Preparing for thumbnail upload...", null));

        var imageFileId = await GetOrCreateThumbnailFileIdAsync(
            this, previousImageUrl, thumbnailFileName
        );

        var imageFileVersion = await CreateAndUploadFileVersionAsync(
            thumbnailStream,
            imageFileId,
            VRChatApiFileUtils.GetMimeTypeFromExtension(Path.GetExtension(thumbnailFileName)),
            $"{contentType} Thumbnail",
            arg => progressCallback?.Invoke(new PublishTaskProgressEventArg(arg.ProgressText, arg.ProgressValue)),
            cancellationToken
        );

        if (imageFileVersion.File is null)
            throw new UnexpectedApiBehaviourException(
                "Api did not return file info for created image file version.");

        return imageFileVersion.File.Url;
    }

    public async ValueTask<string> GetOrCreateThumbnailFileIdAsync(
        VRChatApiClient apiClient, string? sourceImageUrl, string imageFileName)
    {
        if (sourceImageUrl is not null)
        {
            var fileId = VRChatApiFileUtils.TryGetFileIdFromAssetUrl(sourceImageUrl);
            if (fileId is null)
                throw new UnexpectedApiBehaviourException("Api returned an invalid image asset url.");

            logger.LogInformation(
                "Thumbnail image already exists with file id {FileId}, will reuse this file for thumbnail.", fileId);
            return fileId;
        }

        logger.LogInformation("Thumbnail image does not exist, creating new file for thumbnail upload.");

        var extension = Path.GetExtension(imageFileName);
        var mimeType = VRChatApiFileUtils.GetMimeTypeFromExtension(extension);

        var fileName = Path.GetFileName(imageFileName);
        var file = await apiClient.CreateFileAsync(fileName, mimeType, extension);

        logger.LogInformation("Created new file for thumbnail upload with id {FileId}.", file.Id);
        return file.Id;
    }

    public async ValueTask<VRChatApiFileVersion> CreateAndUploadFileVersionAsync(
        Stream fileStream, string fileId, string contentType,
        string userFileType, Action<PublishTaskProgressEventArg>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentAssetFile = await GetFileAsync(fileId, cancellationToken);

        // Step 1. Cleanup any incomplete file versions.
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Cleanup all incomplete file versions...", null));

        if (!await CleanupIncompleteFileVersionsAsync(currentAssetFile, this, cancellationToken))
        {
            currentAssetFile = await GetFileAsync(fileId, cancellationToken);
        }

        // Step 2. Calculate bundle file md5 and check is same file exists.
        progressCallback?.Invoke(new PublishTaskProgressEventArg($"Calculating MD5 for {userFileType} file...", null));

        var fileMd5 = await VRChatApiFileUtils.GetMd5FromStreamForVRChatAsync(fileStream, cancellationToken);
        var fileLength = fileStream.Length;

        var existingFileVersion = currentAssetFile.Versions
            .Where(version => version.File is { } file && file.Md5 == fileMd5)
            .ToArray();

        if (existingFileVersion.Length > 0)
        {
            logger.LogWarning("File with same MD5 already exists for file {FileId}", fileId);
            if (existingFileVersion.FirstOrDefault(version => version.Status == "complete") is not { } completeVersion)
                throw new UnexpectedApiBehaviourException(
                    "One or more file version with the same md5 already exists but is not complete. " +
                    "Which should not happen since we have cleaned up incomplete file versions.");

            if (completeVersion.Version != currentAssetFile.Versions.Select(v => v.Version).Max())
            {
                throw new InvalidOperationException(
                    "One or more file version with the same md5 already exists and is complete.");
            }

            logger.LogWarning(
                "Existing file version {Version} is already complete for file {FileId}, and it's the latest one, so skipping upload and reuse this file version.",
                completeVersion.Version, fileId);

            return completeVersion;
        }

        // Step 3. Caulate file signature
        progressCallback?.Invoke(new PublishTaskProgressEventArg(
            $"Calculating (Blake2b) Signature for {userFileType} file...",
            null));

        var signatureStream =
            new MemoryStream(
                await VRChatApiFileUtils.GetSignatureFromStreamForVRChatAsync(fileStream, cancellationToken));
        var signatureMd5 = await VRChatApiFileUtils.GetMd5FromStreamForVRChatAsync(signatureStream, cancellationToken);
        var signatureLength = signatureStream.Length;

        logger.LogInformation("Creating new file version for file {FileId}", fileId);

        // Step 4. Create new file version
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Creating new file version...", null));

        var fileVersion =
            await CreateFileVersionAsync(fileId, fileMd5, fileLength, signatureMd5, signatureLength, cancellationToken);

        if (fileVersion.File is null || fileVersion.Signature is null)
            throw new UnexpectedApiBehaviourException(
                "Api did not return file or signature info for created file version.");

        // Step 5. Upload bundle file and signature to aws s3
        logger.LogInformation("Uploading file version {Version} for file {FileId}", fileVersion.Version, fileId);
        progressCallback?.Invoke(new PublishTaskProgressEventArg($"Preparing for Upload {userFileType} file...", null));

        await UploadFileVersionAsync(fileStream, fileId, fileVersion.Version, fileMd5,
            fileVersion.File.Category == "simple", VRChatApiFileType.File, contentType,
            (progress, bytesPerSecond) => progressCallback?.Invoke(
                new PublishTaskProgressEventArg(
                    GetUploadProgressText($"Uploading {userFileType} file...", progress, bytesPerSecond),
                    progress
                )), cancellationToken);

        logger.LogInformation("Uploading signature for {FileId}", fileId);
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Preparing for Upload signature...", null));

        await UploadFileVersionAsync(signatureStream, fileId, fileVersion.Version, signatureMd5,
            fileVersion.Signature.Category == "simple", VRChatApiFileType.Signature, contentType,
            (progress, bytesPerSecond) => progressCallback?.Invoke(
                new PublishTaskProgressEventArg(
                    GetUploadProgressText("Uploading signature file...", progress, bytesPerSecond),
                    progress
                )), cancellationToken);

        // Step 6. Wait for server to process the uploaded file version
        logger.LogInformation("Waiting for server processing of file version {Version} for file {FileId}",
            fileVersion.Version, fileId);
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Waiting for server processing (for 3s)...", null));

        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        logger.LogInformation("Fetching completed file version {Version} for file {FileId}", fileVersion.Version,
            fileId);
        progressCallback?.Invoke(new PublishTaskProgressEventArg("Fetching new file version detail...", null));

        var completedFile = await GetFileAsync(fileId, cancellationToken);
        return completedFile.Versions.FirstOrDefault(ver => ver.Version == fileVersion.Version) ??
               throw new UnexpectedApiBehaviourException(
                   "Api did not return the created file version after upload complete.");
    }

    private async ValueTask UploadFileVersionAsync(Stream fileStream, string fileId, int version, string md5,
        bool isSimpleUpload, VRChatApiFileType fileType, string? contentType,
        Action<double?, long?>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        progressCallback?.Invoke(null, null);
        if (isSimpleUpload)
        {
            var simpleUploadUrl = await GetSimpleUploadUrlAsync(fileId, version, fileType, cancellationToken);
            await PutFileAsync(
                simpleUploadUrl, fileStream, md5, isSimpleUpload, contentType, cancellationToken);
            await CompleteSimpleFileUploadAsync(fileId, version, fileType, cancellationToken);

            progressCallback?.Invoke(1, null);

            return;
        }

        using var uploader =
            concurrentMultipartUploaderFactory.Create(
                fileStream,
                fileId,
                version,
                fileType,
                this,
                GetUploadClient(),
                cancellationToken);
        uploader.ProgressChanged += (_, progress) =>
            progressCallback?.Invoke(progress.ProgressPrcentage, progress.CurrentSpeedBytesPerSecond);

        var eTags = await uploader.UploadAsync();

        await CompleteFilePartUploadAsync(fileId, version, eTags, fileType, cancellationToken);
        progressCallback?.Invoke(1, null);
    }

    private async ValueTask<string> PutFileAsync(
        string uploadUrl,
        Stream stream,
        string? md5 = null,
        bool isSimpleUpload = false,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var content = new StreamContent(stream);
        if (isSimpleUpload)
        {
            if (md5 is null)
                throw new ArgumentNullException(nameof(md5), "MD5 should be provided for simple upload.");

            if (contentType is null)
                throw new ArgumentNullException(nameof(contentType),
                    "Content type should be provided for simple upload.");

            content.Headers.ContentMD5 = Convert.FromBase64String(md5);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };

        using var awsClient = GetUploadClient();
        var response = await awsClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Headers.ETag is null)
            throw new UnexpectedApiBehaviourException("Api did not return an ETag header.");

        return response.Headers.ETag.Tag;
    }

    private static string GetUploadProgressText(string prefix, double? progress, long? bytesPerSecond)
    {
        var progressText = prefix;
        if (progress is not null)
            progressText += $" ({progress:P})";
        if (bytesPerSecond is not null)
            progressText += $" - {bytesPerSecond / 1.048576e+6d:F}MiB/s";

        return progressText;
    }

    private HttpClient GetUploadClient()
    {
        return options.Value.SimpleUploadHttpClientName is null
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(options.Value.SimpleUploadHttpClientName);
    }
}