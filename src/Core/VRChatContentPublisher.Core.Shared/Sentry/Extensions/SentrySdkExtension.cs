using VRChatContentPublisher.Core.Shared.Utils;

namespace VRChatContentPublisher.Core.Shared.Sentry.Extensions;

public static class SentrySdkExtension
{
    extension(SentrySdk)
    {
        public static void InitForApp()
        {
            SentrySdk.Init(options =>
            {
                var distribution =
#if WINDOWS
                "win-x64";
#else
                    OperatingSystem.IsLinux() ? "linux-x64" : "unknown";
#endif

                options.Dsn =
                    "https://410cfecb0ce1c5ff78c89f94145b0803@o4511519891914752.ingest.de.sentry.io/4511519908364368";
                options.IsGlobalModeEnabled = true;
                options.Distribution = distribution;
                options.CacheDirectoryPath = Path.Combine(AppStorageService.GetTempPath(), "sentry-cache");
                options.EnableLogs = true;
                options.AutoSessionTracking = true;
#if DEBUG
                options.Release = $"dev-{AppVersionUtils.GetAppVersion()}+{AppVersionUtils.GetAppCommitHash()}";
                options.Environment = "debug";
#else
            options.Release = AppVersionUtils.GetAppVersion();
#endif
            });
        }
    }
}