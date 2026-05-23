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

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class BootstrapPageViewModel(
    UserSessionManagerService sessionManagerService,
    NavigationService navigationService,
    IWritableOptions<AppSettings> appSettings,
    DesktopNotificationService desktopNotificationService,
    TaskRestoreService taskRestoreService,
    AppLifetimeService appLifetimeService) : PageViewModelBase
{
    [ObservableProperty]
    public partial string LoadingText { get; private set; } =
        LangKeys.Pages_Bootstrap_Loading_Text_Initializing_Application;

    [RelayCommand]
    private async Task Load()
    {
        await appLifetimeService.WaitForHostStartedAsync();

        LoadingText = LangKeys.Pages_Bootstrap_Loading_Text_Logging_In_Accounts;
        await sessionManagerService.RestoreSessionsAsync(
            session =>
            {
                if (session.State == UserSessionState.InvalidSession)
                {
                    _ = desktopNotificationService.SendNotificationAsync(
                        $"Session restored but invalid for user {session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail}"
                    ).AsTask();
                }
            },
            (session, ex) =>
            {
                if (!appSettings.Value.SendNotificationOnStartupSessionRestoreFailed)
                    return;

                _ = desktopNotificationService.SendNotificationAsync(
                    $"Failed to restore session for user {session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail}",
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