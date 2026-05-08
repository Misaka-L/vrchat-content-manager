using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.App.Services.Dialog;
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
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel,
    DialogService dialogService,
    IServiceProvider serviceProvider
) : DialogViewModelBase
{
    public UpdateDownloadProgressViewModel UpdateDownloadProgressViewModel => updateDownloadProgressViewModel;

    public string Version => updateInformation.Version;
    public string Notes => updateInformation.Notes;
    public DateTimeOffset ReleaseDate => updateInformation.ReleaseDate;
    public string BrowserUrl => updateInformation.BrowserUrl;

    public bool IsWaitingForInstall => appUpdateService.UpdateState is AppUpdateServiceState.WaitingForInstall;
    public bool IsIdle => appUpdateService.UpdateState is AppUpdateServiceState.Idle;
    public bool IsDownloading => !IsIdle && !IsWaitingForInstall;
    public bool IsUpdateInstallationSupported => appUpdateService.IsAppUpdateSupported();

    [ObservableProperty] public partial bool IsSafeToShutdown { get; private set; }

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
        if (!IsUpdateInstallationSupported)
            return;

        if (IsWaitingForInstall)
        {
            try
            {
                await appUpdateService.InstallUpdateAsync();
            }
            catch
            {
                // ignored
            }

            return;
        }

        appUpdateService.StartDownloadUpdate(updateInformation);
    }

    [RelayCommand]
    private async Task CancelUpdate()
    {
        RequestClose();
        var result = await dialogService.ShowDialogAsync(
            serviceProvider.GetRequiredService<CancelUpdateConfirmationDialogViewModel>());
        if (result is not true)
            return;

        await appUpdateService.CancelUpdateAsync();
    }

    private void IsSafeToShutdownChanged(object? sender, bool e)
    {
        IsSafeToShutdown = e;
    }

    private void OnUpdateStateChanged(object? sender, AppUpdateServiceState e)
    {
        OnPropertyChanged(nameof(IsWaitingForInstall));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsDownloading));
    }
}

public sealed class UpdateAvailableDialogViewModelFactory(
    AppUpdateService appUpdateService,
    IWritableOptions<AppSettings> appSettings,
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel,
    AppLifetimeService lifetimeService,
    DialogService dialogService,
    IServiceProvider serviceProvider
)
{
    public UpdateAvailableDialogViewModel Create(AppUpdateInformation updateInformation)
    {
        return new UpdateAvailableDialogViewModel(
            updateInformation,
            appUpdateService,
            lifetimeService,
            appSettings,
            updateDownloadProgressViewModel,
            dialogService,
            serviceProvider
        );
    }
}