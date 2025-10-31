using Microsoft.Extensions.Logging;
using VRChatContentManager.Core.Models;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Worlds;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Utils;

namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public sealed class WorldContentPublisher(
    UserSessionService userSessionService,
    ILogger<WorldContentPublisher> logger,
    string worldId,
    string worldName,
    string platform,
    string unityVersion,
    string? worldSignature)
    : IContentPublisher
{
    public event EventHandler<PublishTaskProgressEventArg>? ProgressChanged;

    public string GetContentType() => "world";

    public string GetContentName() => worldName;

    public async ValueTask PublishAsync(Stream bundleFileStream, HttpClient awsClient)
    {
        if (!bundleFileStream.CanRead || !bundleFileStream.CanSeek)
            throw new ArgumentException("The provided stream must be readable and seekable.",
                nameof(bundleFileStream));

        var apiClient = userSessionService.GetApiClient();

        // Step 1. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.

        logger.LogInformation("Publish World {WorldId}", worldId);
        UpdateProgress("Fetching world detail...", null);

        var world = await apiClient.GetWorldAsync(worldId);
        // Find the latest unity package for the specified platform (to get the file id)
        var platformApiUnityPackage = world.UnityPackages
            .Where(package => package.Platform == platform)
            .GroupBy(package => package.UnityVersion)
            .MaxBy(group => UnityVersion.TryParse(group.Key))!
            .MaxBy(package => package.AssetVersion);

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
        UpdateProgress("Preparing for upload bundle file...", null);

        var fileVersion = await apiClient.CreateAndUploadFileVersionAsync(bundleFileStream, fileId, awsClient,
            arg => { UpdateProgress(arg.ProgressText, arg.ProgressValue); });
        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 6. Update World
        logger.LogInformation("Updating world {WorldId} to use new file version {Version}", worldId,
            fileVersion.Version);
        UpdateProgress("Updating world to latest asset version...", null);

        await apiClient.CreateWorldVersionAsync(worldId, new CreateWorldVersionRequest(
            worldName,
            fileVersion.File.Url,
            fileVersion.Version,
            platform,
            unityVersion,
            worldSignature
        ));

        logger.LogInformation("Successfully published world {WorldId}", worldId);
        UpdateProgress("World Published", 1, ContentPublishTaskStatus.Completed);
    }

    private void UpdateProgress(string text, double? value, ContentPublishTaskStatus status = ContentPublishTaskStatus.InProgress)
    {
        ProgressChanged?.Invoke(this, new PublishTaskProgressEventArg(text, value, status));
    }
}

public sealed class WorldContentPublisherFactory(ILogger<WorldContentPublisher> logger)
{
    public WorldContentPublisher Create(
        UserSessionService userSessionService,
        string worldId,
        string worldName,
        string platform,
        string unityVersion,
        string? worldSignature)
    {
        return new WorldContentPublisher(userSessionService, logger, worldId, worldName, platform, unityVersion,
            worldSignature);
    }
}