using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Models;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.UnityPackages;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Worlds;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Services.VRChatApi;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;

public sealed class WorldContentPublisher(
    string worldId,
    string worldName,
    string platform,
    string unityVersion,
    string? worldSignature,
    int? capacity,
    int? recommendedCapacity,
    string? previewYoutubeId,
    string[]? udonProducts,
    UserSessionService userSessionService,
    ILogger<WorldContentPublisher> logger,
    IFileService tempFileService)
    : IContentPublisher
{
    private readonly string[] _udonProducts = udonProducts ?? [];

    private readonly VRChatApiClient _apiClient = userSessionService.GetApiClient();

    public string GetContentType() => "world";

    public string GetContentName() => worldName;
    public string GetContentPlatform() => platform;

    public async ValueTask BeforePublishTaskAsync(
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        HttpClient awsClient,
        CancellationToken cancellationToken = default
    )
    {
        // try fetch world detail, if not found means we need to create a new world.
        try
        {
            await _apiClient.GetWorldAsync(worldId);
            return;
        }
        catch (ApiErrorException ex) when (ex.StatusCode == 404)
        {
            logger.LogInformation("The world {WorldId} was not found. Creating new world.", worldId);
        }

        logger.LogInformation("Uploading thumbnail file for creating new world {WorldId}", worldId);
        if (thumbnailFileId is null)
            throw new InvalidOperationException("Thumbnail must be provided when creating a new world.");

        var thumbnailFile = await tempFileService.GetFileWithNameAsync(thumbnailFileId);
        await using var thumbnailFileStream = thumbnailFile?.FileStream;

        if (thumbnailFile is null || thumbnailFileStream is null)
            throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));

        var imageUrl = await UploadThumbnailFileAsync(null, thumbnailFile, awsClient, null, cancellationToken);
        
        logger.LogInformation("Send create world request for {WorldId}", worldId);
        await _apiClient.CreateWorldAsync(new CreateWorldRequest(
            worldId,
            worldName,
            null,
            null,
            null,
            null,
            null,
            imageUrl,
            description,
            tags,
            releaseStatus,
            capacity,
            recommendedCapacity,
            previewYoutubeId,
            null
        ), cancellationToken);
    }

    private const long MaxBundleFileSizeForMobileBytes = 104857600; // 100 MB

    public async ValueTask PublishAsync(
        string bundleFileId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        HttpClient awsClient,
        PublishStageProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        await using var bundleFileStream = await tempFileService.GetFileAsync(bundleFileId);
        var thumbnailFile = thumbnailFileId is not null
            ? await tempFileService.GetFileWithNameAsync(thumbnailFileId)
            : null;
        await using var thumbnailFileStream = thumbnailFile?.FileStream;

        if (bundleFileStream is null)
            throw new InvalidOperationException("Could not find the provided bundle file.");

        if (thumbnailFile is null && thumbnailFileId is not null)
            throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));

        if (!UnityBuildTargetUtils.IsStandalonePlatform(platform) &&
            bundleFileStream.Length > MaxBundleFileSizeForMobileBytes)
            throw new ArgumentException(
                "The provided bundle file exceeds the maximum allowed size of 100 MB for this platform.",
                nameof(bundleFileId));

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Publish World {WorldId}", worldId);
        progressReporter?.Report("Fetching world detail...");

        // Step 1. Fetch world detail, it should be always exist since pre publish will ensure this.
        var world = await _apiClient.GetWorldAsync(worldId);

        // Step 2. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.
        var fileId = await GetOrCreateBundleFileIdAsync(world);

        logger.LogInformation("Using file id {FileId} for world {WorldId}", fileId, worldId);
        progressReporter?.Report("Preparing for upload bundle file...");

        // Step 3. Create and upload a new file version
        var fileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
            bundleFileStream,
            fileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(".vrcw"),
            awsClient,
            "World Bundle",
            arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue)
            , cancellationToken
        );

        // Step 3.1 Upload thumbnail if needed
        string? imageUri = null;
        if (thumbnailFile is not null && thumbnailFileStream is not null)
        {
            logger.LogInformation("Uploading thumbnail for world {AvatarId}", worldId);
            progressReporter?.Report("Uploading thumbnail...");

            imageUri = await UploadThumbnailFileAsync(world, thumbnailFile, awsClient, progressReporter,
                cancellationToken);
        }

        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 4. Update World
        logger.LogInformation("Updating world {WorldId} to use new file version {Version}", worldId,
            fileVersion.Version);
        progressReporter?.Report("Updating world to latest asset version...");

        await _apiClient.CreateWorldVersionAsync(worldId, new CreateWorldVersionRequest(
            worldName,
            fileVersion.File.Url,
            fileVersion.Version,
            platform,
            unityVersion,
            worldSignature,
            imageUri,
            description,
            tags,
            releaseStatus,
            capacity,
            recommendedCapacity,
            previewYoutubeId,
            _udonProducts
        ), cancellationToken);

        logger.LogInformation("Successfully published world {WorldId}", worldId);
    }

    private VRChatApiUnityPackage? TryGetUnityPackageForPlatform(VRChatApiWorld world)
    {
        return world.UnityPackages
            .Where(package => package.Platform == platform)
            .GroupBy(package => package.UnityVersion)
            .MaxBy(group => UnityVersion.TryParse(group.Key))?
            .MaxBy(package => package.AssetVersion);
    }

    private async ValueTask<string> GetOrCreateBundleFileIdAsync(VRChatApiWorld? apiWorld)
    {
        if (apiWorld is not null)
        {
            var platformApiUnityPackage = TryGetUnityPackageForPlatform(apiWorld);
            if (platformApiUnityPackage is not null)
            {
                var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(platformApiUnityPackage.AssetUrl);
                if (fileId is null)
                    throw new UnexpectedApiBehaviourException("Api returned an invalid asset url.");

                return fileId;
            }
        }

        var fileName = $"World - {worldName} - Asset bundle - {unityVersion}-{platform}";
        var file = await _apiClient.CreateFileAsync(fileName, "application/x-world", ".vrcw");
        return file.Id;
    }

    private async ValueTask<string> UploadThumbnailFileAsync(
        VRChatApiWorld? world,
        UploadedFile thumbnailFile,
        HttpClient awsClient,
        PublishStageProgressReporter? progressReporter,
        CancellationToken cancellationToken = default
    )
    {
        var imageFileId = await GetOrCreateBundleImageIdAsync(world, thumbnailFile.FileName);

        var imageFileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
            thumbnailFile.FileStream,
            imageFileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(Path.GetExtension(thumbnailFile.FileName)),
            awsClient,
            "World Thumbnail",
            arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue), cancellationToken
        );

        if (imageFileVersion.File is null)
            throw new UnexpectedApiBehaviourException(
                "Api did not return file info for created image file version.");

        return imageFileVersion.File.Url;
    }

    private async ValueTask<string> GetOrCreateBundleImageIdAsync(VRChatApiWorld? apiWorld, string imageFileName)
    {
        if (apiWorld?.ImageUrl is not null)
        {
            var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(apiWorld.ImageUrl);
            if (fileId is null)
                throw new UnexpectedApiBehaviourException("Api returned an invalid image asset url.");

            return fileId;
        }

        var extension = Path.GetExtension(imageFileName);
        var mimeType = VRChatApiFlieUtils.GetMimeTypeFromExtension(extension);

        var fileName = $"World - {worldName} - Image - {unityVersion}-{platform}";
        var file = await _apiClient.CreateFileAsync(fileName, mimeType, extension);
        return file.Id;
    }
}

public sealed class WorldContentPublisherFactory(ILogger<WorldContentPublisher> logger, IFileService tempFileService)
{
    public WorldContentPublisher Create(
        UserSessionService userSessionService,
        string worldId,
        string worldName,
        string platform,
        string unityVersion,
        string? worldSignature,
        int? capacity,
        int? recommendedCapacity,
        string? previewYoutubeId,
        string[]? udonProducts)
    {
        return new WorldContentPublisher(
            worldId,
            worldName,
            platform,
            unityVersion,
            worldSignature,
            capacity,
            recommendedCapacity,
            previewYoutubeId,
            udonProducts,
            userSessionService,
            logger,
            tempFileService
        );
    }
}