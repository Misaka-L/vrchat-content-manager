using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideWelcomePageViewModel(
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory) : PageViewModelBase
{
    [RelayCommand]
    private void NavigateToHomePage()
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void NextPage()
    {
        var addAccountPageViewModel = addAccountPageViewModelFactory.Create(
            navigationService.Navigate<GuideWelcomePageViewModel>,
            navigationService.Navigate<GuideSetupUnityPageViewModel>);

        navigationService.Navigate(addAccountPageViewModel);
    }
}