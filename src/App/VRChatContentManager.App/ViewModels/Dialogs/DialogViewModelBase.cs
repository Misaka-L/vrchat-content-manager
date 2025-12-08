using System;
using CommunityToolkit.Mvvm.Input;

namespace VRChatContentManager.App.ViewModels.Dialogs;

public abstract partial class DialogViewModelBase : ViewModelBase
{
    public event EventHandler<object?>? CloseRequested;

    [RelayCommand]
    protected void RequestClose(object? arg = null)
    {
        CloseRequested?.Invoke(this, arg);
    }
}