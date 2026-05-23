using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Notification;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideWelcomePageViewModel(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    LoginPageViewModelFactory loginPageViewModelFactory,
    InAppNotificationService inAppNotificationService,
    IWritableOptions<AppSettings> appSettings) : PageViewModelBase
{
    public string AppVersion => AppVersionUtils.GetAppVersion();

    [RelayCommand]
    private void NavigateToHomePage()
    {
        if (!appSettings.Value.SkipFirstSetup)
        {
            inAppNotificationService.SendNotification<SkipOnboardingOnFirstStartAppNotificationViewModel>();
        }

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