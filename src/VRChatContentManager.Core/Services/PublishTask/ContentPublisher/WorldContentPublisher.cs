using Microsoft.Extensions.Logging;
using VRChatContentManager.ConnectCore.Services;
using VRChatContentManager.Core.Models;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.UnityPackages;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Worlds;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Utils;

namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public sealed class WorldContentPublisher(
    string worldId,
    string worldName,
    string platform,
    string unityVersion,
    string? worldSignature,
    UserSessionService userSessionService,
    ILogger<WorldContentPublisher> logger,
    IFileService tempFileService
) : IContentPublisher
{
    public string GetContentType() => "world";

    public string GetContentName() => worldName;
    public string GetContentPlatform() => platform;

    private const long MaxBundleFileSizeForMobileBytes = 104857600; // 100 MB

    // TODO: Implement thumbnail, description, tags, release status handling
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
        if (bundleFileStream is null)
            throw new InvalidOperationException("Could not find the provided bundle file.");

        if (!UnityBuildTargetUtils.IsStandalonePlatform(platform) &&
            bundleFileStream.Length > MaxBundleFileSizeForMobileBytes)
            throw new ArgumentException(
                "The provided bundle file exceeds the maximum allowed size of 100 MB for this platform.",
                nameof(bundleFileId));

        cancellationToken.ThrowIfCancellationRequested();

        var apiClient = userSessionService.GetApiClient();

        // Step 1. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.

        logger.LogInformation("Publish World {WorldId}", worldId);
        progressReporter?.Report("Fetching world detail...");

        var world = await apiClient.GetWorldAsync(worldId);
        // Find the latest unity package for the specified platform (to get the file id)
        var platformApiUnityPackage = TryGetUnityPackageForPlatform(world);

        if (platformApiUnityPackage is null)
        {
            logger.LogError("Could not find Unity package for platform {Platform} in world {WorldId}", platform,
                worldId);
            throw new NotImplementedException();
        }

        var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(platformApiUnityPackage.AssetUrl);
        if (fileId is null)
            throw new UnexpectedApiBehaviourException("Api returned an invalid asset url.");

        logger.LogInformation("Using file id {FileId} for world {WorldId}", fileId, worldId);
        progressReporter?.Report("Preparing for upload bundle file...");

        // Step 2. Create and upload a new file version
        var fileVersion = await apiClient.CreateAndUploadFileVersionAsync(
            bundleFileStream,
            fileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(".vrcw"),
            awsClient,
            "World Bundle",
            arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue)
            , cancellationToken
        );

        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 3. Update World
        logger.LogInformation("Updating world {WorldId} to use new file version {Version}", worldId,
            fileVersion.Version);
        progressReporter?.Report("Updating world to latest asset version...");

        await apiClient.CreateWorldVersionAsync(worldId, new CreateWorldVersionRequest(
            worldName,
            fileVersion.File.Url,
            fileVersion.Version,
            platform,
            unityVersion,
            worldSignature
        ), cancellationToken);

        logger.LogInformation("Successfully published world {WorldId}", worldId);
    }

    private VRChatApiUnityPackage? TryGetUnityPackageForPlatform(VRChatApiWorld world)
    {
        return world.UnityPackages
            .Where(package => package.Platform == platform)
            .GroupBy(package => package.UnityVersion)
            .MaxBy(group => UnityVersion.TryParse(group.Key))!
            .MaxBy(package => package.AssetVersion);
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
        string? worldSignature)
    {
        return new WorldContentPublisher(
            worldId,
            worldName,
            platform,
            unityVersion,
            worldSignature,
            userSessionService,
            logger,
            tempFileService
        );
    }
}