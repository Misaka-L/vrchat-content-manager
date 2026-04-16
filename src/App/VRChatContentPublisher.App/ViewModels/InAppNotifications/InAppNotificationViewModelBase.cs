using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public abstract partial class InAppNotificationViewModelBase : ViewModelBase
{
    public event EventHandler? CloseRequested;

    [RelayCommand]
    protected void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}