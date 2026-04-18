using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.Data;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed partial class UpdateProgressAppNotificationViewModel(
    AppUpdateService appUpdateService,
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel
) : InAppNotificationViewModelBase
{
    public UpdateDownloadProgressViewModel UpdateDownloadProgressViewModel => updateDownloadProgressViewModel;

    public string StateColor => appUpdateService.UpdateState switch
    {
        AppUpdateServiceState.DownloadError or AppUpdateServiceState.IntegrityCheckFailed => "#d50000",
        _ => "#3f51b5"
    };

    [RelayCommand]
    private void Load()
    {
        appUpdateService.OnUpdateStateChanged += OnUpdateStateChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        appUpdateService.OnUpdateStateChanged -= OnUpdateStateChanged;
    }

    private void OnUpdateStateChanged(object? sender, AppUpdateServiceState e)
    {
        OnPropertyChanged(nameof(StateColor));
    }
}