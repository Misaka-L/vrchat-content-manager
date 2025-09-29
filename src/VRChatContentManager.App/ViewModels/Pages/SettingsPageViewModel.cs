using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    UserSessionManagerService userSessionManagerService) : PageViewModelBase
{
    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}