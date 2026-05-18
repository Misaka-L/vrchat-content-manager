using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
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
    TaskRestoreService taskRestoreService) : PageViewModelBase
{
    [RelayCommand]
    private async Task Load()
    {
        await sessionManagerService.RestoreSessionsAsync((session, ex) =>
        {
            if (!appSettings.Value.SendNotificationOnStartupSessionRestoreFailed)
                return;

            _ = desktopNotificationService.SendNotificationAsync(
                $"Failed to restore session for user {session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail}",
                ex.Message
            ).AsTask();
        });

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