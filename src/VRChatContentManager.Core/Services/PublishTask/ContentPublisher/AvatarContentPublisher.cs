using Microsoft.Extensions.Logging;
using VRChatContentManager.Core.Models;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Avatars;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Utils;

namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public sealed class AvatarContentPublisher(
    UserSessionService userSessionService,
    ILogger<AvatarContentPublisher> logger,
    string avatarId,
    string name,
    string platform,
    string unityVersion
) : IContentPublisher
{
    public event EventHandler<PublishTaskProgressEventArg>? ProgressChanged;

    public string GetContentType() => "avatar";
    public string GetContentName() => name;
    public string GetContentPlatform() => platform;

    private const long MaxBundleFileSizeForDesktopBytes = 209715200; // 200 MB
    private const long MaxBundleFileSizeForMobileBytes = 10485760; // 10 MB

    public async ValueTask PublishAsync(Stream bundleFileStream, HttpClient awsClient)
    {
        if (!bundleFileStream.CanRead || !bundleFileStream.CanSeek)
            throw new ArgumentException("The provided stream must be readable and seekable.",
                nameof(bundleFileStream));

        if (UnityBuildTargetUtils.IsStandalonePlatform(platform))
        {
            if (bundleFileStream.Length > MaxBundleFileSizeForDesktopBytes)
                throw new ArgumentException(
                    "The provided bundle file exceeds the maximum allowed size of 200 MB for this platform.",
                    nameof(bundleFileStream));
        }
        else
        {
            if (bundleFileStream.Length > MaxBundleFileSizeForMobileBytes)
                throw new ArgumentException(
                    "The provided bundle file exceeds the maximum allowed size of 10 MB for this platform.",
                    nameof(bundleFileStream));
        }

        var apiClient = userSessionService.GetApiClient();

        // Step 1. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.

        logger.LogInformation("Publish Avatar {AvatarId}", avatarId);
        UpdateProgress("Fetching avatar detail...", null);

        var avatar = await apiClient.GetAvatarAsync(avatarId);
        // Find the latest unity package for the specified platform (to get the file id)
        var platformApiUnityPackage = avatar.UnityPackages
            .Where(package => package.Platform == platform)
            .GroupBy(package => package.UnityVersion)
            .MaxBy(group => UnityVersion.TryParse(group.Key))!
            .MaxBy(package => package.AssetVersion);

        if (platformApiUnityPackage is null)
        {
            logger.LogError("Could not find Unity package for platform {Platform} in avatar {AvatarId}", platform,
                avatarId);
            throw new NotImplementedException();
        }

        var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(platformApiUnityPackage.AssetUrl);
        if (fileId is null)
            throw new UnexpectedApiBehaviourException("Api returned an invalid asset url.");

        logger.LogInformation("Using file id {FileId} for avatar {AvatarId}", fileId, avatarId);
        UpdateProgress("Preparing for upload bundle file...", null);

        var fileVersion = await apiClient.CreateAndUploadFileVersionAsync(bundleFileStream, fileId, awsClient,
            arg => { UpdateProgress(arg.ProgressText, arg.ProgressValue); });
        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 6. Update Avatar
        logger.LogInformation("Updating avatar {AvatarId} to use new file version {Version}", avatarId,
            fileVersion.Version);
        UpdateProgress("Updating avatar to latest asset version...", null);

        await apiClient.CreateAvatarVersionAsync(avatarId, new CreateAvatarVersionRequest(
            name,
            fileVersion.File.Url,
            1,
            platform,
            unityVersion
        ));

        logger.LogInformation("Successfully published avatar {AvatarId}", avatarId);
        UpdateProgress("Avatar Published", 1, ContentPublishTaskStatus.Completed);
    }

    private void UpdateProgress(string text, double? value,
        ContentPublishTaskStatus status = ContentPublishTaskStatus.InProgress)
    {
        ProgressChanged?.Invoke(this, new PublishTaskProgressEventArg(text, value, status));
    }
}

public sealed class AvatarContentPublisherFactory(ILogger<AvatarContentPublisher> logger)
{
    public AvatarContentPublisher Create(
        UserSessionService userSession,
        string avatarId,
        string name,
        string platform,
        string unityVersion)
    {
        return new AvatarContentPublisher(userSession, logger, avatarId, name, platform, unityVersion);
    }
}