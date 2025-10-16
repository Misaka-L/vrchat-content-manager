using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using VRChatContentManager.Core.Services.PublishTask;

namespace VRChatContentManager.App.ViewModels.Data.PublishTasks;

public sealed class PublishTaskManagerViewModel : ViewModelBase
{
    public string UserDisplayName { get; }
    public ObservableCollection<PublishTaskViewModel> Tasks { get; } = [];

    public PublishTaskManagerViewModel(TaskManagerService taskManagerService, PublishTaskViewModelFactory taskFactory,
        string userDisplayName)
    {
        UserDisplayName = userDisplayName;
        
        taskManagerService.TaskCreated += (_, task) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var viewModel = taskFactory.Create(task);
                Tasks.Add(viewModel);
            });
        };

        var viewModels = taskManagerService.Tasks.Select(task =>
            taskFactory.Create(task.Value)).ToArray();

        foreach (var viewModel in viewModels)
        {
            Tasks.Add(viewModel);
        }
    }
}

public sealed class PublishTaskManagerViewModelFactory(PublishTaskViewModelFactory taskFactory)
{
    public PublishTaskManagerViewModel Create(TaskManagerService taskManagerService, string userDisplayName) =>
        new(taskManagerService, taskFactory, userDisplayName);
}