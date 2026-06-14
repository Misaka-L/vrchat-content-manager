using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.TelemetryCore;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class PrivacyPolicyConsentPageViewModel(
    NavigationService navigationService,
    AppLifetimeService appLifetimeService,
    PrivacyPolicyService privacyPolicyService
) : PageViewModelBase
{
    [ObservableProperty] public partial string PrivacyPolicyUrl { get; private set; } = string.Empty;

    [ObservableProperty] public partial int PrivacyPolicyVersion { get; private set; }

    [ObservableProperty] public partial bool IsUpdate { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnableTelemetry))]
    [NotifyPropertyChangedFor(nameof(IsFullMode))]
    private partial TelemetryMode SelectedTelemetryMode { get; set; } = TelemetrySettings.TelemetryMode;

    public bool IsEnableTelemetry
    {
        get => SelectedTelemetryMode != TelemetryMode.Disabled;
        set => SelectedTelemetryMode = value ? TelemetryMode.PrivacyMode : TelemetryMode.Disabled;
    }

    public bool IsFullMode
    {
        get => SelectedTelemetryMode == TelemetryMode.All;
        set => SelectedTelemetryMode = value ? TelemetryMode.All : TelemetryMode.PrivacyMode;
    }

    [RelayCommand]
    private async Task Load()
    {
        var latestPolicy = await privacyPolicyService.GetLatestPrivacyPolicyAsync();
        PrivacyPolicyVersion = latestPolicy.Version;
        PrivacyPolicyUrl = privacyPolicyService.GetLocalizedPolicyUrl(latestPolicy);

        var storedVersion = TelemetrySettings.UserAgreementVersion;

        // Already agreed and up to date — skip directly to bootstrap
        if (storedVersion is not null && storedVersion >= PrivacyPolicyVersion)
        {
            navigationService.Navigate<BootstrapPageViewModel>();
            return;
        }

        IsUpdate = storedVersion is not null;
    }

    [RelayCommand]
    private void AgreeAndContinue()
    {
        TelemetrySettings.UserAgreementVersion = PrivacyPolicyVersion;
        TelemetrySettings.TelemetryMode = SelectedTelemetryMode;

        navigationService.Navigate<BootstrapPageViewModel>();
    }

    [RelayCommand]
    private void DisagreeAndExit()
    {
        appLifetimeService.Shutdown();
    }
}