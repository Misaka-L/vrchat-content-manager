using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideBuildAndUploadPageViewModel(
    NavigationService navigationService
) : PageViewModelBase
{
    /// <summary>
    /// Set when this page is opened from somewhere other than the onboarding flow (e.g. RPC server
    /// settings). When not <see langword="null"/> a back button is shown and invokes this action.
    /// </summary>
    public Action? OnRequestBackOverride { get; set; }

    public bool CanGoBack => OnRequestBackOverride is not null;

    [RelayCommand]
    private void Back()
    {
        OnRequestBackOverride?.Invoke();
    }

    [RelayCommand]
    private void Done()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}
