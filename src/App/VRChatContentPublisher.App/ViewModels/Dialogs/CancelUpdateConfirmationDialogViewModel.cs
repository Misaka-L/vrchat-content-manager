using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class CancelUpdateConfirmationDialogViewModel : DialogViewModelBase
{
    [RelayCommand]
    private void ConfirmCancel()
    {
        RequestClose(true);
    }
}
