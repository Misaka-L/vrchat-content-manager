using MessagePipe;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Models;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher.Options;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.Core.Utils;
using VRChatContentPublisher.Core.VRChatApi;
using VRChatContentPublisher.Core.VRChatApi.Exceptions;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.UnityPackages;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Worlds;

namespace VRChatContentPublisher.Core.ContentPublishing.ContentPublisher;

#pragma warning disable CS9124 // options captured into closure — expected for Options pattern
public sealed class WorldContentPublisher(
    WorldContentPublisherOptions options,
    UserSessionService userSessionService,
    ILogger<WorldContentPublisher> logger,
    IFileService fileService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber
) : IContentPublisher
{
    internal WorldContentPublisherOptions Options { get; } = options;

    private readonly string[] _udonProducts = options.UdonProducts ?? [];

    private readonly VRChatApiClient _apiClient = userSessionService.GetApiClient();

    public string GetContentType() => "world";

    public string GetContentName() => options.WorldName;
    public string GetContentPlatform() => options.Platform;

    public bool CanPublish()
    {
        return userSessionService.State == UserSessionState.LoggedIn;
    }

    public async ValueTask BeforePublishTaskAsync(
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        CancellationToken cancellationToken = default
    )
    {
        // try fetch world detail, if not found means we need to create a new world.
        try
        {
            await _apiClient.GetWorldAsync(options.WorldId);
            return;
        }
        catch (ApiErrorException ex) when (ex.StatusCode == 404)
        {
            logger.LogInformation("The world {WorldId} was not found. Creating new world.", options.WorldId);
        }

        logger.LogInformation("Uploading thumbnail file for creating new world {WorldId}", options.WorldId);
        if (thumbnailFileId is null)
            throw new InvalidOperationException("Thumbnail must be provided when creating a new world.");

        var thumbnailFile = await fileService.GetFileWithNameAsync(thumbnailFileId);
        await using var thumbnailFileStream = thumbnailFile?.FileStream;

        if (thumbnailFile is null || thumbnailFileStream is null)
            throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));

        var imageUrl = await UploadThumbnailFileAsync(null, thumbnailFile, null, cancellationToken);

        logger.LogInformation("Send create world request for {WorldId}", options.WorldId);
        await _apiClient.CreateWorldAsync(new CreateWorldRequest(
            options.WorldId,
            options.WorldName,
            null,
            null,
            null,
            null,
            null,
            imageUrl,
            description,
            tags,
            releaseStatus,
            options.Capacity,
            options.RecommendedCapacity,
            options.PreviewYoutubeId,
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
        PublishStageProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        using var sessionValidScope = new EnsureSessionValidScope(
            userSessionService.UserNameOrEmail,
            sessionStateChangedSubscriber,
            cancellationToken
        );

        cancellationToken = sessionValidScope.CancellationToken;

        await using var bundleFileStream = await fileService.GetFileAsync(bundleFileId);
        var thumbnailFile = thumbnailFileId is not null
            ? await fileService.GetFileWithNameAsync(thumbnailFileId)
            : null;
        await using var thumbnailFileStream = thumbnailFile?.FileStream;

        if (bundleFileStream is null)
            throw new InvalidOperationException("Could not find the provided bundle file.");

        if (thumbnailFile is null && thumbnailFileId is not null)
            throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));

        if (!UnityBuildTargetUtils.IsStandalonePlatform(options.Platform) &&
            bundleFileStream.Length > MaxBundleFileSizeForMobileBytes)
            throw new ArgumentException(
                "The provided bundle file exceeds the maximum allowed size of 100 MB for this platform.",
                nameof(bundleFileId));

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Publish World {WorldId}", options.WorldId);
        progressReporter?.Report("Fetching world detail...");

        // Step 1. Fetch world detail, it should be always exist since pre publish will ensure this.
        var world = await _apiClient.GetWorldAsync(options.WorldId);

        // Step 2. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.
        var fileId = await GetOrCreateBundleFileIdAsync(world);

        logger.LogInformation("Using file id {FileId} for world {WorldId}", fileId, options.WorldId);
        progressReporter?.Report("Preparing for upload bundle file...");

        // Step 3. Create and upload a new file version
        var fileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
            bundleFileStream,
            fileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(".vrcw"),
            "World Bundle",
            arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue)
            , cancellationToken
        );

        // Step 3.1 Upload thumbnail if needed
        string? imageUri = null;
        if (thumbnailFile is not null && thumbnailFileStream is not null)
        {
            logger.LogInformation("Uploading thumbnail for world {AvatarId}", options.WorldId);
            progressReporter?.Report("Uploading thumbnail...");

            imageUri = await UploadThumbnailFileAsync(world, thumbnailFile, progressReporter, cancellationToken);
        }

        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 4. Update World
        logger.LogInformation("Updating world {WorldId} to use new file version {Version}", options.WorldId,
            fileVersion.Version);
        progressReporter?.Report("Updating world to latest asset version...");

        await _apiClient.CreateWorldVersionAsync(options.WorldId, new CreateWorldVersionRequest(
            options.WorldName,
            fileVersion.File.Url,
            fileVersion.Version,
            options.Platform,
            options.UnityVersion,
            options.WorldSignature,
            imageUri,
            description,
            tags,
            releaseStatus,
            options.Capacity,
            options.RecommendedCapacity,
            options.PreviewYoutubeId,
            _udonProducts
        ), cancellationToken);

        logger.LogInformation("Successfully published world {WorldId}", options.WorldId);
    }

    private VRChatApiUnityPackage? TryGetUnityPackageForPlatform(VRChatApiWorld world)
    {
        return world.UnityPackages
            .Where(package => package.Platform == options.Platform)
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

        var fileName = $"World - {options.WorldName} - Asset bundle - {options.UnityVersion}-{options.Platform}";
        var file = await _apiClient.CreateFileAsync(fileName, "application/x-world", ".vrcw");
        return file.Id;
    }

    private async ValueTask<string> UploadThumbnailFileAsync(
        VRChatApiWorld? world,
        UploadedFile thumbnailFile,
        PublishStageProgressReporter? progressReporter,
        CancellationToken cancellationToken = default
    )
    {
        var imageFileId = await GetOrCreateBundleImageIdAsync(world, thumbnailFile.FileName);

        var imageFileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
            thumbnailFile.FileStream,
            imageFileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(Path.GetExtension(thumbnailFile.FileName)),
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

        var fileName = $"World - {options.WorldName} - Image - {options.UnityVersion}-{options.Platform}";
        var file = await _apiClient.CreateFileAsync(fileName, mimeType, extension);
        return file.Id;
    }
}

public sealed class WorldContentPublisherFactory(
    ILogger<WorldContentPublisher> logger,
    IFileService fileService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber
)
{
    public WorldContentPublisher Create(
        UserSessionService userSessionService,
        WorldContentPublisherOptions options)
    {
        return new WorldContentPublisher(
            options,
            userSessionService,
            logger,
            fileService,
            sessionStateChangedSubscriber
        );
    }
}