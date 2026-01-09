using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Settings;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    AccountsSettingsViewModel accountsSettingsViewModel,
    ConnectSettingsViewModel connectSettingsViewModel,
    SessionsSettingsViewModel sessionsSettingsViewModel,
    AboutSettingsViewModel aboutSettingsViewModel,
    HttpProxySettingsViewModel httpProxySettingsViewModel,
    DebugSettingsViewModel debugSettingsViewModel) : PageViewModelBase
{
    public AccountsSettingsViewModel AccountsSettingsViewModel { get; } = accountsSettingsViewModel;
    public ConnectSettingsViewModel ConnectSettingsViewModel { get; } = connectSettingsViewModel;
    public SessionsSettingsViewModel SessionsSettingsViewModel { get; } = sessionsSettingsViewModel;
    public HttpProxySettingsViewModel HttpProxySettingsViewModel { get; } = httpProxySettingsViewModel;
    public AboutSettingsViewModel AboutSettingsViewModel { get; } = aboutSettingsViewModel;
    public DebugSettingsViewModel DebugSettingsViewModel { get; } = debugSettingsViewModel;
    
    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}