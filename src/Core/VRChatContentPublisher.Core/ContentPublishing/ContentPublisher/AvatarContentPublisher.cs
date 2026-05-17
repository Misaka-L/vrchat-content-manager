using MessagePipe;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher.Options;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask;
using VRChatContentPublisher.Core.Events.UserSession;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.Core.Utils;
using VRChatContentPublisher.Core.VRChatApi;
using VRChatContentPublisher.Core.VRChatApi.Exceptions;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Avatars;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.UnityPackages;

namespace VRChatContentPublisher.Core.ContentPublishing.ContentPublisher;

#pragma warning disable CS9124 // options captured into closure — expected for Options pattern
public sealed class AvatarContentPublisher(
    AvatarContentPublisherOptions options,
    UserSessionService userSessionService,
    ILogger<AvatarContentPublisher> logger,
    IFileService fileService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber
) : IContentPublisher
{
    internal AvatarContentPublisherOptions Options { get; } = options;

    private readonly VRChatApiClient _apiClient = userSessionService.GetApiClient();

    public string GetContentType() => "avatar";
    public string GetContentName() => options.Name;
    public string GetContentPlatform() => options.Platform;

    public bool CanPublish()
    {
        return userSessionService.State == UserSessionState.LoggedIn;
    }

    public ValueTask BeforePublishTaskAsync(string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        CancellationToken cancellationToken = default)
    {
        // Do nothing
        return ValueTask.CompletedTask;
    }

    private const long MaxBundleFileSizeForDesktopBytes = 209715200; // 200 MB
    private const long MaxBundleFileSizeForMobileBytes = 10485760; // 10 MB

    public async ValueTask PublishAsync(string bundleFileId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        PublishStageProgressReporter progressReporter,
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
            throw new ArgumentException("Could not find the provided bundle file.", nameof(bundleFileId));

        if (thumbnailFile is null && thumbnailFileId is not null)
            throw new ArgumentException("Could not find the provided thumbnail file.", nameof(thumbnailFileId));

        if (UnityBuildTargetUtils.IsStandalonePlatform(options.Platform))
        {
            if (bundleFileStream.Length > MaxBundleFileSizeForDesktopBytes)
                throw new ArgumentException(
                    "The provided bundle file exceeds the maximum allowed size of 200 MB for this platform.",
                    nameof(bundleFileId));
        }
        else
        {
            if (bundleFileStream.Length > MaxBundleFileSizeForMobileBytes)
                throw new ArgumentException(
                    "The provided bundle file exceeds the maximum allowed size of 10 MB for this platform.",
                    nameof(bundleFileId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1. Try to get the asset file for this platform, if not create a new one.
        // This step also cleanups any incomplete file versions.

        logger.LogInformation("Publish Avatar {AvatarId}", options.AvatarId);
        progressReporter.Report("Fetching avatar detail...");

        var avatar = await _apiClient.GetAvatarAsync(options.AvatarId, cancellationToken);
        var fileId = await GetOrCreateBundleFileIdAsync(avatar);

        // Step 2. Create and upload a new file version
        logger.LogInformation("Using file id {FileId} for avatar {AvatarId}", fileId, options.AvatarId);
        progressReporter.Report("Preparing for upload bundle file...");

        var fileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
            bundleFileStream,
            fileId,
            VRChatApiFlieUtils.GetMimeTypeFromExtension(".vrca"),
            "Avatar Bundle",
            arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue), cancellationToken
        );

        if (fileVersion.File is null)
            throw new UnexpectedApiBehaviourException("Api did not return file info for created file version.");

        // Step 2.1 Upload thumbnail if needed
        string? imageUri = null;
        if (thumbnailFile is not null && thumbnailFileStream is not null)
        {
            logger.LogInformation("Uploading thumbnail for avatar {AvatarId}", options.AvatarId);
            progressReporter?.Report("Uploading thumbnail...");

            var imageFileId = await GetOrCreateBundleImageIdAsync(avatar, thumbnailFile.FileName);

            var imageFileVersion = await _apiClient.CreateAndUploadFileVersionAsync(
                thumbnailFileStream,
                imageFileId,
                VRChatApiFlieUtils.GetMimeTypeFromExtension(Path.GetExtension(thumbnailFile.FileName)),
                "Avatar Thumbnail",
                arg => progressReporter?.Report(arg.ProgressText, arg.ProgressValue), cancellationToken
            );

            if (imageFileVersion.File is null)
                throw new UnexpectedApiBehaviourException(
                    "Api did not return file info for created image file version.");

            imageUri = imageFileVersion.File.Url;
        }

        // Step 3. Update Avatar
        logger.LogInformation("Updating avatar {AvatarId} to use new file version {Version}", options.AvatarId,
            fileVersion.Version);
        progressReporter?.Report("Updating avatar to latest asset version...");

        await _apiClient.CreateAvatarVersionAsync(options.AvatarId, new CreateAvatarVersionRequest(
            options.Name,
            fileVersion.File.Url,
            1,
            options.Platform,
            options.UnityVersion,
            imageUri,
            description,
            tags,
            releaseStatus
        ), cancellationToken);

        logger.LogInformation("Successfully published avatar {AvatarId}", options.AvatarId);
    }

    private VRChatApiUnityPackage? TryGetUnityPackageForPlatform(VRChatApiAvatar apiAvatar)
    {
        var platformApiUnityPackage = apiAvatar.UnityPackages
            .Where(package => package.Platform == options.Platform)
            .GroupBy(package => package.UnityVersion)
            .MaxBy(group => UnityVersion.TryParse(group.Key))?
            .MaxBy(package => package.AssetVersion);

        return platformApiUnityPackage;
    }

    private async ValueTask<string> GetOrCreateBundleFileIdAsync(VRChatApiAvatar apiAvatar)
    {
        var platformApiUnityPackage = TryGetUnityPackageForPlatform(apiAvatar);
        if (platformApiUnityPackage is not null)
        {
            var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(platformApiUnityPackage.AssetUrl);
            if (fileId is null)
                throw new UnexpectedApiBehaviourException("Api returned an invalid asset url.");

            return fileId;
        }

        var fileName = $"Avatar - {options.Name} - Asset bundle - {options.UnityVersion}-{options.Platform}";
        var file = await _apiClient.CreateFileAsync(fileName, "application/x-avatar", ".vrca");
        return file.Id;
    }

    private async ValueTask<string> GetOrCreateBundleImageIdAsync(VRChatApiAvatar apiAvatar, string imageFileName)
    {
        if (apiAvatar.ImageUrl is not null)
        {
            var fileId = VRChatApiFlieUtils.TryGetFileIdFromAssetUrl(apiAvatar.ImageUrl);
            if (fileId is null)
                throw new UnexpectedApiBehaviourException("Api returned an invalid image asset url.");

            return fileId;
        }

        var extension = Path.GetExtension(imageFileName);
        var mimeType = VRChatApiFlieUtils.GetMimeTypeFromExtension(extension);

        var fileName = $"Avatar - {options.Name} - Image - {options.UnityVersion}-{options.Platform}";
        var file = await _apiClient.CreateFileAsync(fileName, mimeType, extension);
        return file.Id;
    }
}

public sealed class AvatarContentPublisherFactory(
    ILogger<AvatarContentPublisher> logger,
    IFileService fileService,
    ISubscriber<SessionStateChangedEvent> sessionStateChangedSubscriber
)
{
    public AvatarContentPublisher Create(
        UserSessionService userSession,
        AvatarContentPublisherOptions options)
    {
        return new AvatarContentPublisher(
            options,
            userSession,
            logger,
            fileService,
            sessionStateChangedSubscriber
        );
    }
}