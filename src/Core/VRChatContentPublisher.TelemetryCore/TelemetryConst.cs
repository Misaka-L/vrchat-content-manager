using VRChatContentPublisher.Core.Shared;

namespace VRChatContentPublisher.TelemetryCore;

public static class TelemetryConst
{
    public static string GetSentryCachePath()
    {
        return CreateIfPathNotExist(Path.Combine(AppStorageService.GetTempPath(), "sentry-cache"));
    }

    public static string GetTelemetryDataPath()
    {
        return CreateIfPathNotExist(Path.Combine(AppStorageService.GetStoragePath(), "telemetry"));
    }

    private static string CreateIfPathNotExist(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }
}