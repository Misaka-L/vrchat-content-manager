using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public partial class GuideSetupUnityPageViewModel(
    NavigationService navigationService,
    ClientSessionService clientSessionService
) : PageViewModelBase
{
    public string VpmRepositoryUrl => "https://project-vrcz.github.io/vpm-listing/index.json";
    public string PackageManagerUrl => "vcc://vpm/addRepo?url=" + Uri.EscapeDataString(VpmRepositoryUrl);

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
    private void Next()
    {
        navigationService.Navigate<GuideOpenConnectSettingsPageViewModel>();
    }
}