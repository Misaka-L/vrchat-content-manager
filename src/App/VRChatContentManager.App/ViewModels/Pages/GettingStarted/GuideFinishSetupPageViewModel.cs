using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;

namespace VRChatContentManager.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideFinishSetupPageViewModel(NavigationService navigationService) : PageViewModelBase
{
    [RelayCommand]
    private void Finish()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}