using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideWelcomePageViewModel(
    UserSessionManagerService userSessionManagerService,
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
        if (userSessionManagerService.Sessions.Count != 0)
        {
            navigationService.Navigate<GuideSetupUnityPageViewModel>();
            return;
        }

        var addAccountPageViewModel = loginPageViewModelFactory.Create(
            navigationService.Navigate<GuideWelcomePageViewModel>,
            navigationService.Navigate<GuideSetupUnityPageViewModel>);

        navigationService.Navigate(addAccountPageViewModel);
    }

    [RelayCommand]
    private void OpenSettingsPage()
    {
        navigationService.Navigate<SettingsPageViewModel>(settings =>
            settings.OnRequestBackOverride = navigationService.Navigate<GuideWelcomePageViewModel>
        );
    }
}