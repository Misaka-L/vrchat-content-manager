using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed partial class SkipOnboardingOnFirstStartAppNotificationViewModel(
    NavigationService navigationService
) : InAppNotificationViewModelBase
{
    [RelayCommand]
    private void RestartOnboarding()
    {
        navigationService.Navigate<GuideWelcomePageViewModel>();
        RequestClose();
    }
}