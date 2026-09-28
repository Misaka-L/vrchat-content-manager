using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Settings;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    LazySettingsSectionViewModelFactory sectionFactory) : PageViewModelBase
{
    public Action? OnRequestBackOverride { get; set; }

    public LazySettingsSectionViewModel<AccountsSettingsViewModel> AccountsSection { get; } =
        sectionFactory.Create<AccountsSettingsViewModel>();

    public LazySettingsSectionViewModel<AppearanceSettingsViewModel> AppearanceSection { get; } =
        sectionFactory.Create<AppearanceSettingsViewModel>();

    public LazySettingsSectionViewModel<ConnectSettingsViewModel> ConnectSection { get; } =
        sectionFactory.Create<ConnectSettingsViewModel>();

    public LazySettingsSectionViewModel<NotificationSettingsViewModel> NotificationSection { get; } =
        sectionFactory.Create<NotificationSettingsViewModel>();

    public LazySettingsSectionViewModel<SessionsSettingsViewModel> SessionsSection { get; } =
        sectionFactory.Create<SessionsSettingsViewModel>();

    public LazySettingsSectionViewModel<HttpProxySettingsViewModel> HttpProxySection { get; } =
        sectionFactory.Create<HttpProxySettingsViewModel>();

    public LazySettingsSectionViewModel<UpdateSettingsViewModel> UpdateSection { get; } =
        sectionFactory.Create<UpdateSettingsViewModel>();

    public LazySettingsSectionViewModel<DebugSettingsViewModel> DebugSection { get; } =
        sectionFactory.Create<DebugSettingsViewModel>();

    public LazySettingsSectionViewModel<TelemetrySettingsViewModel> TelemetrySection { get; } =
        sectionFactory.Create<TelemetrySettingsViewModel>();

    public LazySettingsSectionViewModel<AboutSettingsViewModel> AboutSection { get; } =
        sectionFactory.Create<AboutSettingsViewModel>();

    [RelayCommand]
    private void NavigateToHome()
    {
        if (OnRequestBackOverride is not null)
        {
            OnRequestBackOverride();
            return;
        }

        navigationService.Navigate<HomePageViewModel>();
    }
}