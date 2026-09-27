using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.ConnectCore.Services.Connect;

namespace VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;

public partial class GuideSetupUnityPageViewModel(
    NavigationService navigationService,
    ClientSessionService clientSessionService,
    UriLauncherService uriLauncherService,
    DialogService dialogService,
    PackageManagerUnavailableDialogViewModelFactory packageManagerUnavailableDialogFactory
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

    [RelayCommand]
    private async Task AddToPackageManager()
    {
        if (await uriLauncherService.LaunchUriAsync(PackageManagerUrl))
            return;

        await dialogService.ShowDialogAsync(packageManagerUnavailableDialogFactory.Create(VpmRepositoryUrl));
    }
}