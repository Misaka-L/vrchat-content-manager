using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.App.ViewModels;

public sealed partial class AppViewModel(
    AppWindowService appWindowService,
    AppLifetimeService lifetimeService,
    DialogService dialogService,
    ExitAppDialogViewModel exitAppDialogViewModel
) : ViewModelBase
{
    public string LogsFolderPath => AppStorageService.GetLogsPath();

    [RelayCommand]
    private async Task ActivateWindow()
    {
        await appWindowService.ActivateMainWindowAsync();
    }

    [RelayCommand]
    private async Task ExitApp()
    {
        if (!lifetimeService.IsSafeToShutdown())
        {
            await appWindowService.ActivateMainWindowAsync();
            await dialogService.ShowDialogAsync(exitAppDialogViewModel);
            return;
        }

        lifetimeService.Shutdown();
    }
}