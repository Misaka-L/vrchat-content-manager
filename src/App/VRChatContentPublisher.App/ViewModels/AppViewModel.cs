using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
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

    public bool IsBorderless => appWindowService.IsBorderless();
    public bool IsPinned => appWindowService.IsPinned();

    public string ToggleBorderlessItemText => IsBorderless
        ? LangKeys.Tray_Menu_Borderless_Window_Toggle_Checked_Text
        : LangKeys.Tray_Menu_Borderless_Window_Toggle_Unchecked_Text;

    public string TogglePinnedItemText => IsPinned
        ? LangKeys.Tray_Menu_Pinned_Window_Toggle_Checked_Text
        : LangKeys.Tray_Menu_Pinned_Window_Toggle_Unchecked_Text;

    [RelayCommand]
    private void Load()
    {
        appWindowService.IsBorderlessChanged += IsBorderlessChanged;
        appWindowService.IsPinnedChanged += IsPinnedChanged;
    }

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

    [RelayCommand]
    private async Task ToggleBorderlessWindow()
    {
        await appWindowService.SetBorderlessAsync(!IsBorderless);
    }

    [RelayCommand]
    private void TogglePinnedWindow()
    {
        appWindowService.SetPin(!IsPinned);
    }

    private void IsPinnedChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(TogglePinnedItemText));
    }

    private void IsBorderlessChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsBorderless));
        OnPropertyChanged(nameof(ToggleBorderlessItemText));
    }
}