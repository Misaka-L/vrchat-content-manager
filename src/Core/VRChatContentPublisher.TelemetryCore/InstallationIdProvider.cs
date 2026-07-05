using System.Security.Cryptography;
using System.Text;

namespace VRChatContentPublisher.TelemetryCore;

public static class InstallationIdProvider
{
    private static readonly string InstallationIdPath =
        Path.Combine(TelemetryConst.GetTelemetryDataPath(), "installation_id");

    private static string? _installationIdCached;
    private static readonly Lock InstallationIdLock = new();

    public static string GetInstallationId()
    {
        lock (InstallationIdLock)
        {
            if (_installationIdCached != null)
                return _installationIdCached;

            if (File.Exists(InstallationIdPath))
            {
                _installationIdCached = File.ReadAllText(InstallationIdPath);
                return _installationIdCached;
            }

            if (TryGetSentryPersistentInstallationId() is { } sentryInstallationId)
            {
                File.WriteAllText(InstallationIdPath, sentryInstallationId);
                _installationIdCached = sentryInstallationId;
                return _installationIdCached;
            }

            var newInstallationId = Guid.NewGuid().ToString();
            File.WriteAllText(InstallationIdPath, newInstallationId);
            _installationIdCached = newInstallationId;

            return _installationIdCached;
        }
    }

    // https://github.com/getsentry/sentry-dotnet/blob/0061920452318c78877e0441abcede7fd10743e4/src/Sentry/Internal/InstallationIdHelper.cs#L50-L97
    private static string? TryGetSentryPersistentInstallationId()
    {
        if (SentryDsnProvider.GetDsn() is not { } dsn)
            return null;

        var sentryCacheRootPath = TelemetryConst.GetSentryCachePath();
        var installationIdPath =
            Path.Combine(sentryCacheRootPath, "Sentry", ComputeSentryHashString(dsn), ".installation");

        if (!File.Exists(installationIdPath))
            return null;
        return File.ReadAllText(installationIdPath);
    }

    // https://github.com/getsentry/sentry-dotnet/blob/3d6f266e80c956a2fee2e8aaeeaad31dc438110d/src/Sentry/Internal/Extensions/HashExtensions.cs#L5-L11
    // https://github.com/getsentry/sentry-dotnet/blob/3d6f266e80c956a2fee2e8aaeeaad31dc438110d/src/Sentry/Internal/Extensions/MiscExtensions.cs#L15-L37
    private static string ComputeSentryHashString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}