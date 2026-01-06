using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public sealed partial class PublishTaskManagerViewModel(
    UserSessionService userSessionService,
    TaskManagerService taskManagerService,
    PublishTaskViewModelFactory taskFactory,
    SettingsFixAccountPageViewModelFactory fixAccountPageViewModelFactory,
    NavigationService navigationService,
    string userDisplayName)
    : ViewModelBase, IPublishTaskManagerViewModel
{
    public string UserDisplayName { get; } = userDisplayName;
    public AvaloniaList<PublishTaskViewModel> Tasks { get; } = [];

    public bool IsInteractionAllowed => UserSessionService.State == UserSessionState.LoggedIn;

    public UserSessionService UserSessionService => userSessionService;

    [RelayCommand]
    private void Load()
    {
        var viewModels = taskManagerService.Tasks.Select(task =>
                taskFactory.Create(task.Value, taskManagerService))
            .ToArray();

        Tasks.Clear();
        Tasks.AddRange(viewModels);

        OnPropertyChanged(nameof(IsInteractionAllowed));

        taskManagerService.TaskCreated += OnTaskCreated;
        taskManagerService.TaskRemoved += OnTaskRemoved;

        userSessionService.StateChanged += OnUserSessionStateChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        taskManagerService.TaskCreated -= OnTaskCreated;
        taskManagerService.TaskRemoved -= OnTaskRemoved;

        userSessionService.StateChanged -= OnUserSessionStateChanged;
    }

    [RelayCommand]
    private void RemoveCompletedTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Completed)
            .ToArray();

        foreach (var task in completedTasks)
        {
            taskManagerService.RemoveTask(task.TaskId);
        }
    }

    [RelayCommand]
    private void RemoveFailedTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Failed)
            .ToArray();

        foreach (var task in completedTasks)
        {
            taskManagerService.RemoveTask(task.TaskId);
        }
    }

    [RelayCommand]
    private void RemoveCancelledTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Canceled)
            .ToArray();

        foreach (var task in completedTasks)
        {
            taskManagerService.RemoveTask(task.TaskId);
        }
    }

    [RelayCommand]
    private void RemoveAllRemovableTasks()
    {
        var completedTasks = Tasks
            .Where(t =>
                t.Status is ContentPublishTaskStatus.Completed or
                    ContentPublishTaskStatus.Failed or
                    ContentPublishTaskStatus.Canceled)
            .ToArray();

        foreach (var task in completedTasks)
        {
            taskManagerService.RemoveTask(task.TaskId);
        }
    }

    [RelayCommand]
    private async Task RepairSessionAsync()
    {
        if (await userSessionService.TryRepairAsync())
            return;

        var page = fixAccountPageViewModelFactory.Create(userSessionService);
        navigationService.Navigate(page);
    }

    private void OnTaskCreated(object? _, ContentPublishTaskService task)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Tasks.Any(t => t.TaskId == task.TaskId))
                return;

            var viewModel = taskFactory.Create(task, taskManagerService);
            Tasks.Insert(0, viewModel);
        });
    }

    private void OnTaskRemoved(object? sender, ContentPublishTaskService e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var viewModel = Tasks.FirstOrDefault(t => t.TaskId == e.TaskId);
            if (viewModel != null)
                Tasks.Remove(viewModel);
        });
    }

    private void OnUserSessionStateChanged(object? sender, UserSessionState e)
    {
        OnPropertyChanged(nameof(IsInteractionAllowed));
    }
}

public sealed class PublishTaskManagerViewModelFactory(
    PublishTaskViewModelFactory taskFactory,
    SettingsFixAccountPageViewModelFactory fixAccountPageViewModelFactory,
    NavigationService navigationService)
{
    public PublishTaskManagerViewModel Create(
        UserSessionService userSessionService,
        TaskManagerService taskManagerService,
        string userDisplayName
    ) => new(
        userSessionService,
        taskManagerService,
        taskFactory,
        fixAccountPageViewModelFactory,
        navigationService,
        userDisplayName);
}