using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Telemetry.PrivacyPolicy;
using VRChatContentPublisher.TelemetryCore;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class TelemetrySettingsViewModel(PrivacyPolicyService privacyPolicyService) : ViewModelBase
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
            OnPropertyChanged(nameof(IsFullMode));
        }
    }

    public bool IsFullMode
    {
        get => TelemetrySettings.TelemetryMode == TelemetryMode.All;
        set
        {
            var newMode = value ? TelemetryMode.All : TelemetryMode.PrivacyMode;

            if (TelemetrySettings.TelemetryMode == newMode)
                return;

            OnPropertyChanging();
            TelemetrySettings.TelemetryMode = newMode;
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTelemetryEnabled));
        }
    }

    public string PrivacyPolicyUrl => privacyPolicyService.PrivacyPolicyUrl;
}