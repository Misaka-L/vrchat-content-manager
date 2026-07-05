using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using VRChatContentPublisher.Core.Shared;
using VRChatContentPublisher.TelemetryCore.Extensions;

namespace VRChatContentPublisher.TelemetryCore;

public static class TelemetrySettings
{
    private static TelemetrySettingsData _telemetrySettingsData = new();

    private static readonly string TelemetrySettingsPath =
        Path.Combine(AppStorageService.GetStoragePath(), "telemetry-settings.json");

    public static void Initialize()
    {
        if (File.Exists(TelemetrySettingsPath))
        {
            try
            {
                var telemetrySettingsRaw = File.ReadAllText(TelemetrySettingsPath);
                if (JsonSerializer.Deserialize(
                        telemetrySettingsRaw, TelemetrySettingsJsonContext.Default.TelemetrySettingsData
                    ) is { } telemetrySettingsData)
                {
                    _telemetrySettingsData = telemetrySettingsData;
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to load telemetry settings, using default settings.");
            }
        }

        Save();
    }

    private static void Save()
    {
        try
        {
            var tempSettingsFilePath = Path.Combine(AppStorageService.GetTempPath(), Path.GetRandomFileName());
            var telemetrySettingsRaw = JsonSerializer.Serialize(_telemetrySettingsData,
                TelemetrySettingsJsonContext.Default.TelemetrySettingsData);

            File.WriteAllText(tempSettingsFilePath, telemetrySettingsRaw);
            File.Move(tempSettingsFilePath, TelemetrySettingsPath, true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save telemetry settings.");
        }
    }

    public static TelemetryMode TelemetryMode
    {
        get => _telemetrySettingsData.TelemetryMode;
        set
        {
            _telemetrySettingsData.TelemetryMode = value;
            Save();

            SentrySdk.TryUpdateSentryOptions(options => options.SendDefaultPii = TelemetryMode == TelemetryMode.All);
        }
    }

    public static int? UserAgreementVersion
    {
        get => _telemetrySettingsData.UserAgreementVersion;
        set
        {
            _telemetrySettingsData.UserAgreementVersion = value;
            Save();
        }
    }
}

internal class TelemetrySettingsData
{
    public TelemetryMode TelemetryMode { get; set; } = TelemetryMode.PrivacyMode;
    public int? UserAgreementVersion { get; set; }
}

public enum TelemetryMode
{
    Disabled = 0,
    PrivacyMode = 1,
    All = 2
}

[JsonSerializable(typeof(TelemetrySettingsData))]
internal partial class TelemetrySettingsJsonContext : JsonSerializerContext;