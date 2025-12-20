using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.ConnectCore.Services.Connect;

namespace VRChatContentManager.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideConnectUnityPageViewModel(
    NavigationService navigationService,
    ClientSessionService clientSessionService
) : PageViewModelBase
{
    public string HostUri => "http://localhost:59328";

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
}