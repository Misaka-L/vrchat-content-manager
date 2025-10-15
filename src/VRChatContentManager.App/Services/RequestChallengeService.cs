using System.Threading.Tasks;
using Avalonia.Threading;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.ConnectCore.Services.Connect.Challenge;

namespace VRChatContentManager.App.Services;

public class RequestChallengeService(
    DialogService dialogService,
    RequestChallengeDialogViewModelFactory dialogViewModelFactory) : IRequestChallengeService
{
    public Task RequestChallengeAsync(string code, string clientId, string identityPrompt)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            await dialogService.ShowDialogAsync(dialogViewModelFactory.Create(code, clientId, identityPrompt)).AsTask();
        });
        
        return Task.CompletedTask;
    }
}