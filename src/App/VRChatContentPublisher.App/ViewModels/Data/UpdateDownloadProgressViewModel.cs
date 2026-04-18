using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.Dialogs;

namespace VRChatContentPublisher.App.ViewModels.Data;

public sealed partial class UpdateDownloadProgressViewModel(
    AppUpdateService appUpdateService,
    DialogService dialogService,
    IServiceProvider serviceProvider
) : ViewModelBase
{
    [ObservableProperty] public partial bool ShowWhatNewsButton { get; set; } = true;

    public long TotalFileSize => appUpdateService.TotalFileSize ?? 1;
    public long DownloadedFileSize => appUpdateService.DownloadedFileSize ?? 0;
    public string DownloadPrecent => ((double)DownloadedFileSize / TotalFileSize * 100).ToString("N") + "%";

    public double MebibytePerSecondSpeed => appUpdateService.BytesPerSecondSpeed.HasValue
        ? appUpdateService.BytesPerSecondSpeed.Value / 1.048576e+6
        : 0;

    public string? TargetVersion => appUpdateService.UpdateInformation?.Version;

    public bool IsDownloading => appUpdateService.UpdateState == AppUpdateServiceState.Downloading;
    public bool IsWaitingForInstall => appUpdateService.UpdateState == AppUpdateServiceState.WaitingForInstall;

    public bool IsDownloadError =>
        appUpdateService.UpdateState is AppUpdateServiceState.DownloadError
            or AppUpdateServiceState.IntegrityCheckFailed;

    public string? DownloadError => appUpdateService.LastException?.Message;

    public string StateColor => appUpdateService.UpdateState switch
    {
        AppUpdateServiceState.DownloadError or AppUpdateServiceState.IntegrityCheckFailed => "#d50000",
        _ => "#3f51b5"
    };

    private readonly DispatcherTimer _updateProgressTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

    [RelayCommand]
    private void Load()
    {
        appUpdateService.OnUpdateStateChanged += OnUpdateStateChanged;
        _updateProgressTimer.Tick += OnUpdateProgressTimerTick;
        UpdateTimerEnabled();
    }

    [RelayCommand]
    private void Unload()
    {
        appUpdateService.OnUpdateStateChanged -= OnUpdateStateChanged;
        _updateProgressTimer.Tick -= OnUpdateProgressTimerTick;
        _updateProgressTimer.Stop();
    }

    [RelayCommand]
    private async Task CancelUpdate()
    {
        await appUpdateService.CancelUpdateAsync();
    }

    [RelayCommand]
    private async Task RetryDownloadUpdate()
    {
        await appUpdateService.RetryUpdateAsync();
    }

    [RelayCommand]
    private async Task InstallUpdate()
    {
        await appUpdateService.InstallUpdateAsync();
    }

    [RelayCommand]
    private async Task ShowWhatNews()
    {
        if (appUpdateService.UpdateInformation is not { } update)
            return;

        var updateAvailableDialogViewModelFactory =
            serviceProvider.GetRequiredService<UpdateAvailableDialogViewModelFactory>();
        await dialogService.ShowDialogAsync(updateAvailableDialogViewModelFactory.Create(update));
    }

    private void OnUpdateProgressTimerTick(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TotalFileSize));
        OnPropertyChanged(nameof(DownloadedFileSize));
        OnPropertyChanged(nameof(DownloadPrecent));
        OnPropertyChanged(nameof(MebibytePerSecondSpeed));
    }

    private void OnUpdateStateChanged(object? sender, AppUpdateServiceState e)
    {
        OnPropertyChanged(nameof(TargetVersion));

        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsWaitingForInstall));
        OnPropertyChanged(nameof(IsDownloadError));
        OnPropertyChanged(nameof(DownloadError));
        OnPropertyChanged(nameof(StateColor));
        UpdateTimerEnabled();
    }

    private void UpdateTimerEnabled()
    {
        _updateProgressTimer.IsEnabled = appUpdateService.UpdateState == AppUpdateServiceState.Downloading;
    }
}