using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideOpenConnectSettingsPageViewModel(
    NavigationService navigationService,
    ClientSessionService clientSessionService
) : PageViewModelBase
{
    /// <summary>
    /// Set when this page is opened from somewhere other than the onboarding flow (e.g. RPC server
    /// settings). When not <see langword="null"/> a back button is shown and invokes this action.
    /// </summary>
    public Action? OnRequestBackOverride { get; set; }

    public bool CanGoBack => OnRequestBackOverride is not null;

    [RelayCommand]
    private void Load()
    {
        clientSessionService.SessionCreated += OnSessionCreated;
    }

    [RelayCommand]
    private void Unload()
    {
        clientSessionService.SessionCreated -= OnSessionCreated;
    }

    private void OnSessionCreated(object? sender, string e)
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void Skip()
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void Back()
    {
        OnRequestBackOverride?.Invoke();
    }

    [RelayCommand]
    private void Next()
    {
        if (OnRequestBackOverride is not { } requestBack)
        {
            navigationService.Navigate<GuideConnectUnityPageViewModel>();
            return;
        }

        // Entered from outside the onboarding flow: keep the back chain so the user can return to
        // the previous guide page, and from there back to where they came from.
        navigationService.Navigate<GuideConnectUnityPageViewModel>(viewModel =>
            viewModel.OnRequestBackOverride = () =>
                navigationService.Navigate<GuideOpenConnectSettingsPageViewModel>(page =>
                    page.OnRequestBackOverride = requestBack));
    }
}
