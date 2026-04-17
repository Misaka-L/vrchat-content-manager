using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Settings;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    AccountsSettingsViewModel accountsSettingsViewModel,
    AppearanceSettingsViewModel appearanceSettingsViewModel,
    ConnectSettingsViewModel connectSettingsViewModel,
    NotificationSettingsViewModel notificationSettingsViewModel,
    SessionsSettingsViewModel sessionsSettingsViewModel,
    AboutSettingsViewModel aboutSettingsViewModel,
    HttpProxySettingsViewModel httpProxySettingsViewModel,
    DebugSettingsViewModel debugSettingsViewModel,
    UpdateSettingsViewModel updateSettingsViewModel) : PageViewModelBase
{
    public AccountsSettingsViewModel AccountsSettingsViewModel { get; } = accountsSettingsViewModel;
    public AppearanceSettingsViewModel AppearanceSettingsViewModel { get; } = appearanceSettingsViewModel;
    public ConnectSettingsViewModel ConnectSettingsViewModel { get; } = connectSettingsViewModel;
    public NotificationSettingsViewModel NotificationSettingsViewModel { get; } = notificationSettingsViewModel;
    public SessionsSettingsViewModel SessionsSettingsViewModel { get; } = sessionsSettingsViewModel;
    public HttpProxySettingsViewModel HttpProxySettingsViewModel { get; } = httpProxySettingsViewModel;
    public AboutSettingsViewModel AboutSettingsViewModel { get; } = aboutSettingsViewModel;
    public DebugSettingsViewModel DebugSettingsViewModel { get; } = debugSettingsViewModel;
    public UpdateSettingsViewModel UpdateSettingsViewModel { get; } = updateSettingsViewModel;
    
    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}