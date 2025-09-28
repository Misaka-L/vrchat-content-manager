using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;

namespace VRChatContentManager.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(NavigationService navigationService) : PageViewModelBase
{
    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}