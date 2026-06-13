using VRChatContentPublisher.Core.Shared;
using VRChatContentPublisher.Core.Shared.Utils;
using VRChatContentPublisher.TelemetryCore.TelemetryToggle;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class SentrySdkExtension
{
    extension(SentrySdk)
    {
        public static void InitForApp()
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = SentryDsnProvider.GetDsn();
                options.IsGlobalModeEnabled = true;
                options.Distribution = GetDistribution();
                options.CacheDirectoryPath = Path.Combine(AppStorageService.GetTempPath(), "sentry-cache");
                options.EnableLogs = true;
                options.AutoSessionTracking = true;
                options.Release = GetRelease();
                options.DisableSentryHttpMessageHandler = true;
                options.TracesSampleRate = 1.0;
                options.Environment = GetEnvironment();
                options.UseOtlp();

                options.AddTelemetryModeListener();
            });
        }
    }

    internal static string GetEnvironment()
    {
#if DEBUG
        return "debug";
#else
        return "production";
#endif
    }

    private static string GetRelease()
    {
        return $"content-publisher@{AppVersionUtils.GetAppVersion()}+{AppVersionUtils.GetAppCommitHash()}";
    }

    private static string GetDistribution()
    {
#if WINDOWS
        return "win-x64";
#else
        return OperatingSystem.IsLinux() ? "linux-x64" : "unknown";
#endif
    }
}