using VRChatContentPublisher.TelemetryCore;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class TelemetrySettingsViewModel : ViewModelBase
{
    public bool IsTelemetryEnabled
    {
        get => TelemetrySettings.TelemetryMode != TelemetryMode.Disabled;
        set
        {
            var newMode = value ? TelemetryMode.PrivacyMode : TelemetryMode.Disabled;

            if (TelemetrySettings.TelemetryMode == newMode)
                return;

            OnPropertyChanging();
            TelemetrySettings.TelemetryMode = newMode;
            OnPropertyChanged();

            // When toggling telemetry off/on, notify that Full Mode may have changed
            if (!value)
                OnPropertyChanged(nameof(IsFullMode));
        }
    }

    public bool IsFullMode
    {
        get => TelemetrySettings.TelemetryMode == TelemetryMode.All;
        set
        {
            // Full Mode is only meaningful when telemetry is enabled
            if (!IsTelemetryEnabled)
                return;

            var newMode = value ? TelemetryMode.All : TelemetryMode.PrivacyMode;

            if (TelemetrySettings.TelemetryMode == newMode)
                return;

            OnPropertyChanging();
            TelemetrySettings.TelemetryMode = newMode;
            OnPropertyChanged();
        }
    }

    // Placeholder for privacy policy URL — replace with the actual URL later
    public string PrivacyPolicyUrl => "#";
}
