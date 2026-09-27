using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideConnectUnityPageViewModel(
    NavigationService navigationService,
    ClientSessionService clientSessionService,
    IWritableOptions<AppSettings> appSettings
) : PageViewModelBase
{
    /// <summary>
    /// Set when this page is opened from somewhere other than the onboarding flow (e.g. RPC server
    /// settings). When not <see langword="null"/> a back button is shown and invokes this action.
    /// </summary>
    public Action? OnRequestBackOverride { get; set; }

    public bool CanGoBack => OnRequestBackOverride is not null;

    public string HostUri => $"http://localhost:{appSettings.Value.RpcServerPort}";

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
        // Unity is now connected: show the final guide page explaining that publishing only needs
        // the "Build and Upload" button in the VRChat SDK build target panel.
        // Forward the back override so a guide opened from somewhere else (e.g. RPC server settings)
        // can still walk back to where it came from.
        navigationService.Navigate<GuideBuildAndUploadPageViewModel>(viewModel =>
            viewModel.OnRequestBackOverride = OnRequestBackOverride);
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
}
