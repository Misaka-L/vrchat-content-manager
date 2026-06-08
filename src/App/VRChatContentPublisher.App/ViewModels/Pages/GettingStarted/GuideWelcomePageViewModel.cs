using System.Linq;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Notification;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Shared.Utils;
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
    private static readonly AppLang FollowSystemLang =
        new(
            LangKeys.Pages_Settings_Appearance_Language_Selector_Follow_System,
            LangKeys.Pages_Settings_Appearance_Language_Selector_Follow_System
        );

    public AppLang[] AvailableLanguages { get; } =
    [
        FollowSystemLang,
        ..AppLocalizationService.GetLanguages()
    ];

    public AppLang SelectedLanguage
    {
        get
        {
            if (appSettings.Value.AppCulture is null)
                return FollowSystemLang;

            return AvailableLanguages.First(x => x.CultureCode == appSettings.Value.AppCulture);
        }
        set
        {
            if (value == FollowSystemLang)
            {
                if (appSettings.Value.AppCulture is null)
                    return;

                OnPropertyChanging();
                UpdateAppCulture(null);
                OnPropertyChanged();
                return;
            }

            if (appSettings.Value.AppCulture == value.CultureCode)
                return;

            OnPropertyChanging();
            UpdateAppCulture(value.CultureCode);
            OnPropertyChanged();
        }
    }

    private void UpdateAppCulture(string? cultureCode)
    {
        AppLocalizationService.ReloadAppCulture(cultureCode);
        appSettings.Update(settings => settings.AppCulture = cultureCode);
    }

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