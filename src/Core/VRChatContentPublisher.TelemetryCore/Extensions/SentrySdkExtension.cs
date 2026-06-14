using System.Net;
using VRChatContentPublisher.Core.Shared;
using VRChatContentPublisher.Core.Shared.Utils;
using VRChatContentPublisher.TelemetryCore.TelemetryToggle;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class SentrySdkExtension
{
    private static readonly WebProxyWarper WebProxyWarperInstance = new();
    // It looks like SentrySdk won't deep-copy SentryOptions
    private static SentryOptions? _sentryOptions;

    extension(SentrySdk)
    {
        public static void InitForApp()
        {
            SentrySdk.Init(options =>
            {
                _sentryOptions = options;

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
                options.SendDefaultPii = TelemetrySettings.TelemetryMode == TelemetryMode.All;
                options.HttpProxy = WebProxyWarperInstance;
                options.UseOtlp();

                options.AddTelemetryModeListener();
            });
        }

        public static bool TryUpdateSentryOptions(Action<SentryOptions> modifyOptions)
        {
            if (_sentryOptions is null)
                return false;

            modifyOptions(_sentryOptions);
            return true;
        }
        
        public static void UpdateWebProxy(IWebProxy? proxy)
        {
            WebProxyWarperInstance.InnerWebProxy = proxy;
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