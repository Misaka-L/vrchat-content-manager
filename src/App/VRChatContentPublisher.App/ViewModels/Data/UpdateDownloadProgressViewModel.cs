using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Services.PublishTask;

namespace VRChatContentPublisher.App.ViewModels.Data;

public sealed partial class UpdateDownloadProgressViewModel(
    AppUpdateService appUpdateService,
    DialogService dialogService,
    AppLifetimeService lifetimeService,
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstallUpdateButtonText))]
    public partial bool IsSafeToShutdown { get; private set; }

    public bool IsError =>
        appUpdateService.UpdateState is AppUpdateServiceState.DownloadError
            or AppUpdateServiceState.IntegrityCheckFailed
            or AppUpdateServiceState.InstallError;

    public string ErrorTitleText =>
        appUpdateService.UpdateState is AppUpdateServiceState.InstallError
            ? LangKeys.Common_Views_Update_Progress_View_Update_Install_Failed_Title
            : LangKeys.Common_Views_Update_Progress_View_Update_Download_Failed_Title;

    public string? UpdateError => appUpdateService.LastException?.Message;

    public string InstallUpdateButtonText => IsSafeToShutdown
        ? LangKeys.Common_Views_Update_Progress_View_Install_Update_Button
        : LangKeys.Common_Views_Update_Progress_View_Install_Update_Disabled_Due_To_Uncompleted_Tasks;

    private readonly DispatcherTimer _updateProgressTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };

    [RelayCommand]
    private void Load()
    {
        IsSafeToShutdown = lifetimeService.IsSafeToShutdown();

        appUpdateService.OnUpdateStateChanged += OnUpdateStateChanged;
        _updateProgressTimer.Tick += OnUpdateProgressTimerTick;
        lifetimeService.IsSafeToShutdownChanged += IsSafeToShutdownChanged;
        UpdateTimerEnabled();
    }

    [RelayCommand]
    private void Unload()
    {
        appUpdateService.OnUpdateStateChanged -= OnUpdateStateChanged;
        _updateProgressTimer.Tick -= OnUpdateProgressTimerTick;
        lifetimeService.IsSafeToShutdownChanged -= IsSafeToShutdownChanged;
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
        try
        {
            await appUpdateService.InstallUpdateAsync();
        }
        catch
        {
            // ignored
        }
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
        OnPropertyChanged(nameof(IsError));
        OnPropertyChanged(nameof(ErrorTitleText));
        OnPropertyChanged(nameof(UpdateError));
        UpdateTimerEnabled();
    }

    private void UpdateTimerEnabled()
    {
        _updateProgressTimer.IsEnabled = appUpdateService.UpdateState == AppUpdateServiceState.Downloading;
    }

    private void IsSafeToShutdownChanged(object? sender, bool e)
    {
        IsSafeToShutdown = e;
    }
}