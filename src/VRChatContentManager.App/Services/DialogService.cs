using System;
using System.Threading.Tasks;
using DialogHostAvalonia;
using VRChatContentManager.App.ViewModels.Dialogs;

namespace VRChatContentManager.App.Services;

public sealed class DialogService
{
    private string? _dialogHostId;
    
    public void SetDialogHostId(string dialogHostId)
    {
        _dialogHostId = dialogHostId;
    }
    
    public async ValueTask<object?> ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModelBase
    {
        if (_dialogHostId is null)
        {
            throw new InvalidOperationException("DialogHostId is not set. Please call SetDialogHost before showing dialogs.");
        }
        
        dialogViewModel.CloseRequested += (_, e) =>
        {
            DialogHost.Close(_dialogHostId, e);
        };

        return await DialogHost.Show(dialogViewModel);
    }
}