using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.TelemetryCore;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class TelemetrySettingsViewModel : ViewModelBase
{
    private readonly PrivacyPolicyService _privacyPolicyService;

    public TelemetrySettingsViewModel(PrivacyPolicyService privacyPolicyService)
    {
        _privacyPolicyService = privacyPolicyService;
    }

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

    public string PrivacyPolicyUrl => _privacyPolicyService.PrivacyPolicyUrl;
}
