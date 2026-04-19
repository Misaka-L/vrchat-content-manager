using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed partial class UpdateAvailableAppNotificationViewModel(
    AppUpdateInformation updateInformation,
    DialogService dialogService,
    UpdateAvailableDialogViewModelFactory availableDialogFactory
) : InAppNotificationViewModelBase
{
    public string Version => updateInformation.Version;

    [RelayCommand]
    private async Task ShowUpdateDialog()
    {
        await dialogService.ShowDialogAsync(availableDialogFactory.Create(updateInformation));
    }
}

public sealed class UpdateAvailableAppNotificationViewModelFactory(
    DialogService dialogService,
    UpdateAvailableDialogViewModelFactory availableDialogFactory
)
{
    public UpdateAvailableAppNotificationViewModel Create(AppUpdateInformation updateInformation)
    {
        return new UpdateAvailableAppNotificationViewModel(updateInformation, dialogService, availableDialogFactory);
    }
}