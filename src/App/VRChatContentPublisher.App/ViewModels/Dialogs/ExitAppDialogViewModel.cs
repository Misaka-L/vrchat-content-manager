using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services.AppLifetime;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class ExitAppDialogViewModel(AppLifetimeService lifetimeService) : DialogViewModelBase
{
    [RelayCommand]
    private void ExitApp()
    {
        lifetimeService.Shutdown();
    }
}