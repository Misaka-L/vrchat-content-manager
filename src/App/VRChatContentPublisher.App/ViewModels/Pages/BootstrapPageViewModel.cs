using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class BootstrapPageViewModel(
    UserSessionManagerService sessionManagerService,
    NavigationService navigationService,
    IWritableOptions<AppSettings> appSettings) : PageViewModelBase
{
    [RelayCommand]
    private async Task Load()
    {
        await sessionManagerService.RestoreSessionsAsync();

        if (!appSettings.Value.SkipFirstSetup)
        {
            navigationService.Navigate<GuideWelcomePageViewModel>();
            return;
        }

        navigationService.Navigate<HomePageViewModel>();
    }
}