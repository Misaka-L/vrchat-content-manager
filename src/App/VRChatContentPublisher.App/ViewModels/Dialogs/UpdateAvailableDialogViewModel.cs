using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class UpdateAvailableDialogViewModel(
    AppUpdateInformation updateInformation,
    AppUpdateService appUpdateService,
    AppLifetimeService lifetimeService,
    IWritableOptions<AppSettings> appSettings,
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel
) : DialogViewModelBase
{
    public UpdateDownloadProgressViewModel UpdateDownloadProgressViewModel => updateDownloadProgressViewModel;

    public string Version => updateInformation.Version;
    public string Notes => updateInformation.Notes;
    public DateTimeOffset ReleaseDate => updateInformation.ReleaseDate;
    public string BrowserUrl => updateInformation.BrowserUrl;

    public bool IsWaitingForInstall => appUpdateService.UpdateState is AppUpdateServiceState.WaitingForInstall;

    [ObservableProperty] public partial bool IsSafeToShutdown { get; private set; }

    public bool IsUpdateAvailableForDownloadOrInstall =>
        appUpdateService.UpdateState is AppUpdateServiceState.WaitingForInstall or AppUpdateServiceState.Idle;

    public bool ShowSkipVersionButton => appUpdateService.UpdateState is AppUpdateServiceState.Idle;

    public string UpdateButtonText => IsWaitingForInstall
        ? LangKeys.Dialog_Update_Available_Download_Install_Update_Button_Text
        : LangKeys.Dialog_Update_Available_Download_Update_Button_Text;

    [RelayCommand]
    private void Load()
    {
        IsSafeToShutdown = lifetimeService.IsSafeToShutdown();

        updateDownloadProgressViewModel.ShowWhatNewsButton = false;
        appUpdateService.OnUpdateStateChanged += OnUpdateStateChanged;
        lifetimeService.IsSafeToShutdownChanged += IsSafeToShutdownChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        appUpdateService.OnUpdateStateChanged -= OnUpdateStateChanged;
        lifetimeService.IsSafeToShutdownChanged -= IsSafeToShutdownChanged;
    }

    [RelayCommand]
    private async Task MarkVersionAsSkippedAsync()
    {
        await appSettings.UpdateAsync(s => s.SkipVersion = updateInformation.Version);
        RequestClose();
    }

    [RelayCommand]
    private async Task StartDownloadOrInstallUpdate()
    {
        if (IsWaitingForInstall)
        {
            await appUpdateService.InstallUpdateAsync();
            return;
        }

        appUpdateService.StartDownloadUpdate(updateInformation);
    }

    private void IsSafeToShutdownChanged(object? sender, bool e)
    {
        IsSafeToShutdown = e;
    }

    private void OnUpdateStateChanged(object? sender, AppUpdateServiceState e)
    {
        OnPropertyChanged(nameof(IsUpdateAvailableForDownloadOrInstall));
        OnPropertyChanged(nameof(ShowSkipVersionButton));
        OnPropertyChanged(nameof(IsWaitingForInstall));
        OnPropertyChanged(nameof(UpdateButtonText));
    }
}

public sealed class UpdateAvailableDialogViewModelFactory(
    AppUpdateService appUpdateService,
    IWritableOptions<AppSettings> appSettings,
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel,
    AppLifetimeService lifetimeService
)
{
    public UpdateAvailableDialogViewModel Create(AppUpdateInformation updateInformation)
    {
        return new UpdateAvailableDialogViewModel(
            updateInformation,
            appUpdateService,
            lifetimeService,
            appSettings,
            updateDownloadProgressViewModel
        );
    }
}