using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.App.ViewModels;

/// <summary>
/// Bottom status bar of the home page. Aggregates the publish task counts of every account
/// directly from the core task managers, so it does not depend on any page or view model.
/// </summary>
public sealed partial class HomeStatusBarViewModel(
    UserSessionManagerService userSessionManagerService,
    IWritableOptions<AppSettings> appSettings,
    ILogger<HomeStatusBarViewModel> logger) : ViewModelBase
{
    /// <summary>
    /// Subscribed task managers, keyed by the manager so task events can be routed back to the
    /// owning subscription. The status of every known task is tracked separately, because the core
    /// task dictionary is live and must not be enumerated from the UI thread.
    /// </summary>
    private readonly Dictionary<TaskManagerService, TaskManagerSubscription> _subscriptions = [];

    private bool _loaded;

    public string RpcServerPortText => appSettings.Value.RpcServerPort.ToString();

    public int InProgressTaskCount => Aggregate(status =>
        status is ContentPublishTaskStatus.InProgress or ContentPublishTaskStatus.Pending);

    public int CompletedTaskCount => Aggregate(status => status is ContentPublishTaskStatus.Completed);
    public int FailedTaskCount => Aggregate(status => status is ContentPublishTaskStatus.Failed);
    public int CanceledTaskCount => Aggregate(status => status is ContentPublishTaskStatus.Canceled);

    [RelayCommand]
    private async Task Load()
    {
        if (_loaded)
            return;

        _loaded = true;

        userSessionManagerService.SessionCreated += OnSessionCreated;
        userSessionManagerService.SessionRemoved += OnSessionRemoved;

        // Read the current state so the bar is correct as soon as it is shown, then keep it up to
        // date through the subscriptions below. Attaching is idempotent, so a session created while
        // the state is being read is still handled by OnSessionCreated.
        foreach (var session in userSessionManagerService.Sessions.ToArray())
            await AttachSessionAsync(session);

        NotifyTaskCountsChanged();
    }

    [RelayCommand]
    private void Unload()
    {
        if (!_loaded)
            return;

        _loaded = false;

        userSessionManagerService.SessionCreated -= OnSessionCreated;
        userSessionManagerService.SessionRemoved -= OnSessionRemoved;

        foreach (var subscription in _subscriptions.Values.ToArray())
            Detach(subscription);

        _subscriptions.Clear();
    }

    private int Aggregate(Func<ContentPublishTaskStatus, bool> predicate) =>
        _subscriptions.Values.Sum(subscription => subscription.TaskStatuses.Values.Count(predicate));

    private async ValueTask AttachSessionAsync(UserSessionService session)
    {
        if (_subscriptions.Values.Any(subscription => subscription.UserSessionService == session))
            return;

        TaskManagerService taskManagerService;
        try
        {
            var scope = await session.CreateOrGetSessionScopeAsync();
            taskManagerService = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
        }
        catch (Exception ex)
        {
            // Sessions without a usable scope have no tasks to aggregate (e.g. invalid sessions).
            logger.LogWarning(ex,
                "Failed to resolve task manager of session {UserNameOrEmail} for the home status bar",
                session.UserNameOrEmail);
            return;
        }

        // The status bar may have been unloaded while the scope was being resolved.
        if (!_loaded)
            return;

        var subscription = new TaskManagerSubscription(session, taskManagerService);

        // Read the latest status of every task before subscribing to further changes.
        foreach (var task in taskManagerService.Tasks.Values)
            subscription.TaskStatuses[task.TaskId] = task.Status;

        if (!_subscriptions.TryAdd(taskManagerService, subscription))
            return;

        taskManagerService.TaskCreated += OnTaskCreated;
        taskManagerService.TaskRemoved += OnTaskRemoved;
        taskManagerService.TaskUpdated += OnTaskUpdated;
    }

    private void Detach(TaskManagerSubscription subscription)
    {
        if (!_subscriptions.Remove(subscription.TaskManagerService))
            return;

        subscription.TaskManagerService.TaskCreated -= OnTaskCreated;
        subscription.TaskManagerService.TaskRemoved -= OnTaskRemoved;
        subscription.TaskManagerService.TaskUpdated -= OnTaskUpdated;
    }

    private void OnSessionCreated(object? sender, UserSessionService session)
    {
        Dispatcher.UIThread.Post(() => _ = AttachSessionAsync(session));
    }

    private void OnSessionRemoved(object? sender, UserSessionService session)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var subscription = _subscriptions.Values
                .FirstOrDefault(subscription => subscription.UserSessionService == session);
            if (subscription is null)
                return;

            Detach(subscription);
            NotifyTaskCountsChanged();
        });
    }

    private void OnTaskCreated(object? sender, ContentPublishTaskCreatedEventArg e)
    {
        PostTaskStatus(sender, e.Task.TaskId, e.Task.Status);
    }

    private void OnTaskRemoved(object? sender, ContentPublishTaskRemovedEventArg e)
    {
        PostTaskStatus(sender, e.Task.TaskId, null);
    }

    private void OnTaskUpdated(object? sender, ContentPublishTaskUpdateEventArg e)
    {
        PostTaskStatus(sender, e.Task.TaskId, e.Task.Status);
    }

    private void PostTaskStatus(object? sender, string taskId, ContentPublishTaskStatus? status)
    {
        if (sender is not TaskManagerService taskManagerService)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (!_subscriptions.TryGetValue(taskManagerService, out var subscription))
                return;

            if (status is { } currentStatus)
                subscription.TaskStatuses[taskId] = currentStatus;
            else
                subscription.TaskStatuses.Remove(taskId);

            NotifyTaskCountsChanged();
        });
    }

    private void NotifyTaskCountsChanged()
    {
        OnPropertyChanged(nameof(InProgressTaskCount));
        OnPropertyChanged(nameof(CompletedTaskCount));
        OnPropertyChanged(nameof(FailedTaskCount));
        OnPropertyChanged(nameof(CanceledTaskCount));
    }

    private sealed class TaskManagerSubscription(
        UserSessionService userSessionService,
        TaskManagerService taskManagerService)
    {
        public UserSessionService UserSessionService { get; } = userSessionService;
        public TaskManagerService TaskManagerService { get; } = taskManagerService;

        public Dictionary<string, ContentPublishTaskStatus> TaskStatuses { get; } = [];
    }
}
