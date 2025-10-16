using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.ViewModels.Data.PublishTasks;
using VRChatContentManager.Core.Services.PublishTask;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    PublishTaskManagerViewModelFactory managerViewModelFactory,
    UserSessionManagerService userSessionManagerService) : PageViewModelBase
{
    public ObservableCollection<PublishTaskManagerViewModel> TaskManagers { get; } = [];

    [RelayCommand]
    private async Task Load()
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            var scope = await session.CreateOrGetSessionScopeAsync();
            var managerService = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            var managerViewModel = managerViewModelFactory.Create(
                managerService,
                session.CurrentUser?.DisplayName ?? session.UserNameOrEmail
            );
            
            TaskManagers.Add(managerViewModel);
        }
    }
}