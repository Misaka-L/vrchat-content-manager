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
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void Skip()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}