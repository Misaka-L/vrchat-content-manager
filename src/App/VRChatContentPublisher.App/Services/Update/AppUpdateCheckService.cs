using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckService(
    ILogger<AppUpdateCheckService> logger,
    IWritableOptions<AppSettings> appSettings,
    IHttpClientFactory httpClientFactory,
    AppUpdateService appUpdateService
)
{
    private const string StableChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater.json";

    private const string PreviewChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater-beta.json";

    public event EventHandler<AppUpdateInformation>? OnUpdateAvailableToDownload;

    public async ValueTask<AppUpdateInformation?> CheckForUpdateAsync()
    {
        if (appUpdateService.UpdateState != AppUpdateServiceState.Idle)
            return null;

        var update = await GetUpdateInformationAsync();
        var currentVersion = AppVersionUtils.GetAppVersion();
        var versionCompareResult = CompareVersion(update.Version, currentVersion);

        if (versionCompareResult is null)
        {
            logger.LogWarning(
                "Cannot compare update version {UpdateVersion} with current version {CurrentVersion}, skip update check",
                update.Version,
                currentVersion
            );
            return null;
        }

        if (versionCompareResult <= 0)
        {
            logger.LogInformation(
                "Current version {CurrentVersion} is newer than or equal to update version {UpdateVersion}, no update available",
                currentVersion,
                update.Version
            );
            return null;
        }

        if (update.Version == appSettings.Value.SkipVersion)
        {
            logger.LogInformation("Skip Version {Version} is set, skipping update notification", update.Version);
            return null;
        }

        logger.LogInformation("New version {Version} is available", update.Version);

        if (appSettings.Value.DownloadUpdateAtBackground && appUpdateService.IsAppUpdateSupported())
        {
            logger.LogInformation("Starting download update at background");
            appUpdateService.StartDownloadUpdate(update);
        }
        else
        {
            OnUpdateAvailableToDownload?.Invoke(this, update);
        }

        return update;
    }

    private async ValueTask<AppUpdateInformation> GetUpdateInformationAsync()
    {
        using var httpClient = httpClientFactory.CreateClient();
        var information = await httpClient.GetFromJsonAsync<AppUpdateInformation>(
            GetUpdateApiUrl(),
            AppUpdateInformationJsonContext.Default.AppUpdateInformation
        ) ?? throw new UnexpectedApiBehaviourException("Update api returned null response");

        return information;
    }

    private string GetUpdateApiUrl()
    {
        return appSettings.Value.ReceivePreviewUpdate ? PreviewChannelUpdateApiUrl : StableChannelUpdateApiUrl;
    }

    private int? CompareVersion(string version1, string version2)
    {
        if (!TryParseSemanticVersion(version1, out var parsedVersion1))
            return null;
        if (!TryParseSemanticVersion(version2, out var parsedVersion2))
            return null;

        return CompareSemanticVersion(parsedVersion1, parsedVersion2);
    }

    private static int CompareSemanticVersion(SemanticVersion version1, SemanticVersion version2)
    {
        var majorComparison = version1.Major.CompareTo(version2.Major);
        if (majorComparison != 0)
            return majorComparison;

        var minorComparison = version1.Minor.CompareTo(version2.Minor);
        if (minorComparison != 0)
            return minorComparison;

        var patchComparison = version1.Patch.CompareTo(version2.Patch);
        if (patchComparison != 0)
            return patchComparison;

        if (version1.PreRelease.Length == 0 && version2.PreRelease.Length == 0)
            return 0;

        if (version1.PreRelease.Length == 0)
            return 1;

        if (version2.PreRelease.Length == 0)
            return -1;

        var minLength = Math.Min(version1.PreRelease.Length, version2.PreRelease.Length);

        for (var i = 0; i < minLength; i++)
        {
            var identifier1 = version1.PreRelease[i];
            var identifier2 = version2.PreRelease[i];

            var identifier1IsNumeric = int.TryParse(identifier1, out var identifier1AsInt);
            var identifier2IsNumeric = int.TryParse(identifier2, out var identifier2AsInt);

            if (identifier1IsNumeric && identifier2IsNumeric)
            {
                var numberComparison = identifier1AsInt.CompareTo(identifier2AsInt);
                if (numberComparison != 0)
                    return numberComparison;
                continue;
            }

            if (identifier1IsNumeric)
                return -1;

            if (identifier2IsNumeric)
                return 1;

            var textComparison = string.CompareOrdinal(identifier1, identifier2);
            if (textComparison != 0)
                return textComparison;
        }

        return version1.PreRelease.Length.CompareTo(version2.PreRelease.Length);
    }

    private static bool TryParseSemanticVersion(string version, out SemanticVersion parsedVersion)
    {
        parsedVersion = default;

        if (string.IsNullOrWhiteSpace(version))
            return false;

        var normalizedVersion = version.Trim();
        if (normalizedVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalizedVersion = normalizedVersion[1..];

        var metadataStartIndex = normalizedVersion.IndexOf('+');
        var versionWithoutMetadata = metadataStartIndex >= 0
            ? normalizedVersion[..metadataStartIndex]
            : normalizedVersion;

        var prereleaseStartIndex = versionWithoutMetadata.IndexOf('-');
        var coreVersion = prereleaseStartIndex >= 0
            ? versionWithoutMetadata[..prereleaseStartIndex]
            : versionWithoutMetadata;
        var prerelease = prereleaseStartIndex >= 0
            ? versionWithoutMetadata[(prereleaseStartIndex + 1)..].Split('.', StringSplitOptions.RemoveEmptyEntries)
            : [];

        var coreVersionParts = coreVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (coreVersionParts.Length is < 2 or > 3)
            return false;

        if (!int.TryParse(coreVersionParts[0], out var major))
            return false;

        if (!int.TryParse(coreVersionParts[1], out var minor))
            return false;

        var patch = 0;
        if (coreVersionParts.Length == 3 && !int.TryParse(coreVersionParts[2], out patch))
            return false;

        parsedVersion = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    private readonly record struct SemanticVersion(int Major, int Minor, int Patch, string[] PreRelease);
}
