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
    [ObservableProperty]
    public partial string PrivacyPolicyUrl { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial int PrivacyPolicyVersion { get; private set; }

    [ObservableProperty]
    public partial bool IsUpdate { get; private set; }

    [ObservableProperty]
    public partial bool IsEnableTelemetry { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFullMode { get; set; }

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

        TelemetrySettings.TelemetryMode = IsEnableTelemetry
            ? (IsFullMode ? TelemetryMode.All : TelemetryMode.PrivacyMode)
            : TelemetryMode.Disabled;

        navigationService.Navigate<BootstrapPageViewModel>();
    }

    [RelayCommand]
    private void DisagreeAndExit()
    {
        appLifetimeService.Shutdown();
    }
}
