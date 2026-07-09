using VRChatContentPublisher.TelemetryCore.Utils;

namespace VRChatContentPublisher.TelemetryCore;

public static class SentryDsnProvider
{
    private const string SentryDsnRuntimeOptionKey = "VRChatContentPublisher.TelemetryCore.Sentry.Dsn";
    private const string SentryDsnEnvironmentVariableKey = "VRChatContentPublisher_TelemetryCore_Sentry_Dsn";

    public static string GetDsn()
    {
        if (AppContext.GetData(SentryDsnRuntimeOptionKey) is string sentryDsn &&
            !string.IsNullOrWhiteSpace(sentryDsn))
            return sentryDsn;

        if (Environment.GetEnvironmentVariable(SentryDsnEnvironmentVariableKey) is { } sentryDsnFromEnv)
            return sentryDsnFromEnv;

        return "";
    }

    public static SentryDsn? TryGetParsedDsn()
    {
        return SentryDsn.TryParse(GetDsn());
    }
}