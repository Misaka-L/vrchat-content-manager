using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideWelcomePageViewModel(
    NavigationService navigationService,
    LoginPageViewModelFactory loginPageViewModelFactory) : PageViewModelBase
{
    public string AppVersion => AppVersionUtils.GetAppVersion();

    [RelayCommand]
    private void NavigateToHomePage()
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void NextPage()
    {
        var addAccountPageViewModel = loginPageViewModelFactory.Create(
            navigationService.Navigate<GuideWelcomePageViewModel>,
            navigationService.Navigate<GuideSetupUnityPageViewModel>);

        navigationService.Navigate(addAccountPageViewModel);
    }
}