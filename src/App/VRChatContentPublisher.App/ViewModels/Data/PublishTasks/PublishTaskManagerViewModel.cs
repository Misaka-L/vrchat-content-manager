using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public sealed partial class PublishTaskManagerViewModel(
    IWritableOptions<AppSettings> appSettings,
    UserSessionService userSessionService,
    TaskManagerService taskManagerService,
    PublishTaskViewModelFactory taskFactory,
    LoginPageViewModelFactory loginPageViewModelFactory,
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

    public bool IsRetryAllowed => UserSessionService.State == UserSessionState.LoggedIn;

    public bool IsContentPublishAllowed =>
        UserSessionService.CurrentUser?.CanPublishAvatar() == true &&
        UserSessionService.CurrentUser?.CanPublishWorld() == true;

    [NotifyPropertyChangedFor(nameof(CurrentPageSortModeText))]
    [ObservableProperty]
    private partial AppTasksPageSortMode TasksSortMode { get; set; }

    public string CurrentPageSortModeText =>
        TasksSortMode == AppTasksPageSortMode.LatestFirst
            ? LangKeys.Pages_Settings_Appearance_How_Tasks_Order_In_Tasks_Page_Selector_Latest_First
            : LangKeys.Pages_Settings_Appearance_How_Tasks_Order_In_Tasks_Page_Selector_Oldest_First;

    public UserSessionService UserSessionService => userSessionService;

    [RelayCommand]
    private void Load()
    {
        TasksSortMode = appSettings.Value.TasksPageSortMode;

        Tasks.Clear();

        if (TasksSortMode == AppTasksPageSortMode.LatestFirst)
        {
            var viewModels = taskManagerService.Tasks
                .Select(task => taskFactory.Create(task.Value, taskManagerService))
                .Reverse()
                .ToArray();
            Tasks.AddRange(viewModels);
        }
        else
        {
            var viewModels = taskManagerService.Tasks
                .Select(task => taskFactory.Create(task.Value, taskManagerService))
                .ToArray();
            Tasks.AddRange(viewModels);
        }

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

        Tasks.Clear();
    }

    [RelayCommand]
    private async Task ToggleSortMode()
    {
        TasksSortMode = TasksSortMode == AppTasksPageSortMode.LatestFirst
            ? AppTasksPageSortMode.OldestFirst
            : AppTasksPageSortMode.LatestFirst;

        await appSettings.UpdateAsync(s => s.TasksPageSortMode = TasksSortMode);
        ResortTasks();
    }

    private void ResortTasks()
    {
        if (TasksSortMode == AppTasksPageSortMode.LatestFirst)
        {
            var sorted = Tasks.OrderByDescending(t => t.CreatedTime).ToArray();
            Tasks.Clear();
            Tasks.AddRange(sorted);
        }
        else
        {
            var sorted = Tasks.OrderBy(t => t.CreatedTime).ToArray();
            Tasks.Clear();
            Tasks.AddRange(sorted);
        }
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

        var page = loginPageViewModelFactory.Create(
            navigationService.Navigate<HomePageViewModel>,
            navigationService.Navigate<HomePageViewModel>,
            userSessionService.UserNameOrEmail
        );

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
        OnPropertyChanged(nameof(IsRetryAllowed));
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
    IWritableOptions<AppSettings> appSettings,
    PublishTaskViewModelFactory taskFactory,
    LoginPageViewModelFactory loginPageViewModelFactory,
    NavigationService navigationService)
{
    public PublishTaskManagerViewModel Create(
        UserSessionService userSessionService,
        TaskManagerService taskManagerService,
        string userDisplayName
    ) => new(
        appSettings,
        userSessionService,
        taskManagerService,
        taskFactory,
        loginPageViewModelFactory,
        navigationService,
        userDisplayName
    );
}