using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class BootstrapPageViewModel(
    UserSessionManagerService sessionManagerService,
    NavigationService navigationService,
    IWritableOptions<AppSettings> appSettings,
    IDesktopNotificationService desktopNotificationService) : PageViewModelBase
{
    [RelayCommand]
    private async Task Load()
    {
        await sessionManagerService.RestoreSessionsAsync((session, ex) =>
        {
            if (!appSettings.Value.SendNotificationOnStartupSessionRestoreFailed)
                return;

            _ = desktopNotificationService.SendDesktopNotificationAsync(
                $"Failed to restore session for user {session.CurrentUser?.DisplayName ?? session.UserId ?? session.UserNameOrEmail}",
                ex.Message
            );
        });

        if (!appSettings.Value.SkipFirstSetup)
        {
            navigationService.Navigate<GuideWelcomePageViewModel>();
            return;
        }

        navigationService.Navigate<HomePageViewModel>();
    }
}