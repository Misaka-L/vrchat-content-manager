using System.Reflection;
using VRChatContentPublisher.Core.Attributes;

namespace VRChatContentPublisher.Core.Utils;

public static class AppVersionUtils
{
    public static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "snapshot";
    }

    public static string GetAppCommitHash()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var commitHashAttribute = assembly.GetCustomAttribute<GitCommitHashAttribute>();
        return commitHashAttribute?.CommitHash ?? "unknown";
    }
    
    public static DateTimeOffset? GetAppBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buildDateAttribute = assembly.GetCustomAttribute<BuildDateTimeAttribute>();
        return buildDateAttribute?.DateTime;
    }
}