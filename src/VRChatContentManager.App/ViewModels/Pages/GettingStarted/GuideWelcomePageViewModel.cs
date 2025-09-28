using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;

namespace VRChatContentManager.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideWelcomePageViewModel(NavigationService navigationService) : PageViewModelBase
{
    [RelayCommand]
    private void NavigateToHomePage()
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void NextPage()
    {
        navigationService.Navigate<GuideAccountPageViewModel>();
    }
}