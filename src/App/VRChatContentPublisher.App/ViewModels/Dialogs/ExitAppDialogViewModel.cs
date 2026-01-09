using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class ExitAppDialogViewModel(
    UserSessionManagerService sessionManagerService) : DialogViewModelBase
{
    [RelayCommand]
    private async Task Load()
    {
        foreach (var session in sessionManagerService.Sessions)
        {
            try
            {
                var scope = await session.CreateOrGetSessionScopeAsync();
                var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

                if (taskManager.Tasks.Count != 0 &&
                    taskManager.Tasks.Any(task => task.Value.Status != ContentPublishTaskStatus.Completed))
                {
                    return;
                }
            }
            catch
            {
                // ignored
            }
        }

        ExitApp();
    }

    [RelayCommand]
    private void ExitApp()
    {
        RequestClose(true);
    }
}