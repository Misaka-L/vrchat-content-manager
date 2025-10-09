using System.Threading.Tasks;
using Avalonia.Threading;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.ConnectCore.Services;

namespace VRChatContentManager.App.Services;

public class RequestChallengeService(
    DialogService dialogService,
    RequestChallengeDialogViewModelFactory dialogViewModelFactory) : IRequestChallengeService
{
    public Task RequestChallengeAsync(string code, string clientId)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            await dialogService.ShowDialogAsync(dialogViewModelFactory.Create(code, clientId)).AsTask();
        });
        
        return Task.CompletedTask;
    }
}