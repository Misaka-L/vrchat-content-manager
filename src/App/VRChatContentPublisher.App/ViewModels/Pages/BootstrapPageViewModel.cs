using Antelcat.I18N.Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.App.Services.Notification;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.UserSession;
using VRChatContentPublisher.TelemetryCore;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class BootstrapPageViewModel(
    UserSessionManagerService sessionManagerService,
    NavigationService navigationService,
    IWritableOptions<AppSettings> appSettings,
    DesktopNotificationService desktopNotificationService,
    TaskRestoreService taskRestoreService,
    AppLifetimeService appLifetimeService,
    PrivacyPolicyService privacyPolicyService) : PageViewModelBase
{
    [ObservableProperty]
    public partial string LoadingText { get; private set; } =
        LangKeys.Pages_Bootstrap_Loading_Text_Initializing_Application;

    [RelayCommand]
    private async Task Load()
    {
        await appLifetimeService.WaitForHostStartedAsync();

        // Privacy policy consent check — must complete before anything else
        var latestPolicy = await privacyPolicyService.GetLatestPrivacyPolicyAsync();
        var storedVersion = TelemetrySettings.UserAgreementVersion;

        if (storedVersion is null || latestPolicy.Version > storedVersion)
        {
            navigationService.Navigate<PrivacyPolicyConsentPageViewModel>();
            return;
        }

        LoadingText = LangKeys.Pages_Bootstrap_Loading_Text_Logging_In_Accounts;
        await sessionManagerService.RestoreSessionsAsync(
            session =>
            {
                if (session.State == UserSessionState.InvalidSession)
                {
                    _ = desktopNotificationService.SendNotificationAsync(
                        string.Format(
                            I18NExtension.Translate(LangKeys.Notifications_Session_Expired_Or_Invalid_Title_Template) ??
                            "Session expired or invalid for user {0}",
                            session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail
                        )
                    ).AsTask();
                }
            },
            (session, ex) =>
            {
                if (!appSettings.Value.SendNotificationOnStartupSessionRestoreFailed)
                    return;

                _ = desktopNotificationService.SendNotificationAsync(
                    string.Format(
                        I18NExtension.Translate(LangKeys.Notifications_Session_Restore_Failed_Title_Template) ??
                        "Failed to restore session for user {0}",
                        session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail
                    ),
                    ex.Message
                ).AsTask();
            });

        LoadingText = LangKeys.Pages_Bootstrap_Loading_Text_Restoring_Publish_Tasks;
        // Restore persisted publish tasks now that user sessions are available.
        await taskRestoreService.RestoreTasksAsync(sessionManagerService);

        if (!appSettings.Value.SkipFirstSetup)
        {
            navigationService.Navigate<GuideWelcomePageViewModel>();
            return;
        }

        navigationService.Navigate<HomePageViewModel>();
    }
}