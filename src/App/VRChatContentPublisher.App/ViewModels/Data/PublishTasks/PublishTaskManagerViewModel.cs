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

    public bool IsAnyTaskExisting => Tasks.Count > 0;
    public int TotalTaskCount => Tasks.Count;
    public int CompletedTaskCount => Tasks.Count(t => t.Status is ContentPublishTaskStatus.Completed);
    public int FailedTaskCount => Tasks.Count(t => t.Status is ContentPublishTaskStatus.Failed);
    public int CanceledTaskCount => Tasks.Count(t => t.Status is ContentPublishTaskStatus.Canceled);

    public int InProgressTaskCount => Tasks.Count(t =>
        t.Status is ContentPublishTaskStatus.InProgress or ContentPublishTaskStatus.Pending);

    public bool IsInteractionAllowed => UserSessionService.State == UserSessionState.LoggedIn;
    public bool IsContentPublishAllowed => 
        UserSessionService.CurrentUser?.CanPublishAvatar() == true && 
        UserSessionService.CurrentUser?.CanPublishWorld() == true;

    public UserSessionService UserSessionService => userSessionService;

    [RelayCommand]
    private void Load()
    {
        var viewModels = taskManagerService.Tasks.Select(task =>
                taskFactory.Create(task.Value, taskManagerService))
            .ToArray();

        Tasks.Clear();
        Tasks.AddRange(viewModels);

        NotifyUserSessionChanged();
        NotifyTaskCountsChanged();

        taskManagerService.TaskCreated += OnTaskCreated;
        taskManagerService.TaskRemoved += OnTaskRemoved;
        taskManagerService.TaskUpdated += OnTaskUpdated;

        userSessionService.StateChanged += OnUserSessionStateChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        taskManagerService.TaskCreated -= OnTaskCreated;
        taskManagerService.TaskRemoved -= OnTaskRemoved;
        taskManagerService.TaskUpdated -= OnTaskUpdated;

        userSessionService.StateChanged -= OnUserSessionStateChanged;
    }

    [RelayCommand]
    private async Task RemoveCompletedTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Completed)
            .ToArray();

        foreach (var task in completedTasks)
        {
            await taskManagerService.RemoveTaskAsync(task.TaskId);
        }
    }

    [RelayCommand]
    private async Task RemoveFailedTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Failed)
            .ToArray();

        foreach (var task in completedTasks)
        {
            await taskManagerService.RemoveTaskAsync(task.TaskId);
        }
    }

    [RelayCommand]
    private async Task RemoveCancelledTasks()
    {
        var completedTasks = Tasks
            .Where(t => t.Status is ContentPublishTaskStatus.Canceled)
            .ToArray();

        foreach (var task in completedTasks)
        {
            await taskManagerService.RemoveTaskAsync(task.TaskId);
        }
    }

    [RelayCommand]
    private async Task RemoveAllRemovableTasks()
    {
        var completedTasks = Tasks
            .Where(t =>
                t.Status is ContentPublishTaskStatus.Completed or
                    ContentPublishTaskStatus.Failed or
                    ContentPublishTaskStatus.Canceled)
            .ToArray();

        foreach (var task in completedTasks)
        {
            await taskManagerService.RemoveTaskAsync(task.TaskId);
        }
    }

    [RelayCommand]
    private void RetryAllTasks()
    {
        var tasks = Tasks
            .Where(task => task.Status is ContentPublishTaskStatus.Failed or ContentPublishTaskStatus.Canceled)
            .ToArray();

        foreach (var task in tasks)
        {
            task.Start();
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
            NotifyTaskCountsChanged();
        });
    }

    private void OnTaskRemoved(object? sender, ContentPublishTaskService e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var viewModel = Tasks.FirstOrDefault(t => t.TaskId == e.TaskId);
            if (viewModel != null)
                Tasks.Remove(viewModel);

            NotifyTaskCountsChanged();
        });
    }

    private void OnTaskUpdated(object? sender, ContentPublishTaskUpdateEventArg e)
    {
        Dispatcher.UIThread.Invoke(NotifyTaskCountsChanged);
    }

    private void OnUserSessionStateChanged(object? sender, UserSessionState e)
    {
        Dispatcher.UIThread.Invoke(NotifyUserSessionChanged);
    }
    
    private void NotifyUserSessionChanged()
    {
        OnPropertyChanged(nameof(IsInteractionAllowed));
        OnPropertyChanged(nameof(IsContentPublishAllowed));
    }

    private void NotifyTaskCountsChanged()
    {
        OnPropertyChanged(nameof(TotalTaskCount));
        OnPropertyChanged(nameof(CompletedTaskCount));
        OnPropertyChanged(nameof(FailedTaskCount));
        OnPropertyChanged(nameof(CanceledTaskCount));
        OnPropertyChanged(nameof(InProgressTaskCount));
        OnPropertyChanged(nameof(IsAnyTaskExisting));
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