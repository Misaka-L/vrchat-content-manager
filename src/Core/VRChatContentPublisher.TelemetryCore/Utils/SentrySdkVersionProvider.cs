using System.Reflection;

namespace VRChatContentPublisher.TelemetryCore.Utils;

public static class SentrySdkVersionProvider
{
    // https://github.com/getsentry/sentry-dotnet/blob/4b51c11082639a3fe4492e1467031fe9c85ec541/src/Sentry/SdkVersion.cs#L14-L19
    public static SdkVersion GetSdkVersion()
    {
        return new SdkVersion
        {
            Name = "sentry.dotnet",
            Version = typeof(ISentryClient).Assembly.GetAssemblySentryVersion()
        };
    }

    // https://github.com/getsentry/sentry-dotnet/blob/4b51c11082639a3fe4492e1467031fe9c85ec541/src/Sentry/Reflection/AssemblyExtensions.cs#L27-L44
    private static string? GetAssemblySentryVersion(this Assembly assembly)
    {
        try
        {
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrEmpty(informationalVersion))
            {
                return informationalVersion;
            }
        }
        catch
        {
            // Note: on full .NET FX, checking the AssemblyInformationalVersionAttribute could throw an exception,
            // therefore this method uses a try/catch to make sure this method always returns a value
        }

        return assembly.GetName().Version?.ToString();
    }
}