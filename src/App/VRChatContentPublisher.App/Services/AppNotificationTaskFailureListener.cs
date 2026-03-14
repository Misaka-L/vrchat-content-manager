using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.Services;

public sealed class AppNotificationTaskFailureListener(
    UserSessionManagerService userSessionManagerService,
    IDesktopNotificationService desktopNotificationService,
    ILogger<AppNotificationTaskFailureListener> logger)
    : IHostedService
{
    private readonly Dictionary<UserSessionService, TaskManagerService> _sessionTaskManagers = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        userSessionManagerService.SessionCreated += OnSessionCreated;
        userSessionManagerService.SessionRemoved += OnSessionRemoved;

        foreach (var session in userSessionManagerService.Sessions)
        {
            AttachSession(session);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        userSessionManagerService.SessionCreated -= OnSessionCreated;
        userSessionManagerService.SessionRemoved -= OnSessionRemoved;

        foreach (var session in userSessionManagerService.Sessions)
        {
            DetachSession(session);
        }

        _sessionTaskManagers.Clear();
        return Task.CompletedTask;
    }

    private void OnSessionCreated(object? _, UserSessionService session)
    {
        AttachSession(session);
    }

    private void OnSessionRemoved(object? _, UserSessionService session)
    {
        DetachSession(session);
    }

    private void AttachSession(UserSessionService session)
    {
        session.StateChanged += OnSessionStateChanged;

        if (session.State == UserSessionState.LoggedIn)
        {
            _ = TryAttachTaskManagerAsync(session);
        }
    }

    private void DetachSession(UserSessionService session)
    {
        session.StateChanged -= OnSessionStateChanged;
        DetachTaskManager(session);
    }

    private async void OnSessionStateChanged(object? sender, UserSessionState state)
    {
        if (sender is not UserSessionService session)
            return;

        if (state == UserSessionState.LoggedIn)
        {
            await TryAttachTaskManagerAsync(session);
            return;
        }

        DetachTaskManager(session);
    }

    private async Task TryAttachTaskManagerAsync(UserSessionService session)
    {
        if (session.State != UserSessionState.LoggedIn || _sessionTaskManagers.ContainsKey(session))
            return;

        try
        {
            var scope = await session.CreateOrGetSessionScopeAsync();
            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            if (_sessionTaskManagers.TryAdd(session, taskManager))
                taskManager.TaskUpdated += OnTaskUpdated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to attach task update listener for user session {UserNameOrEmail}",
                session.UserNameOrEmail);
        }
    }

    private void DetachTaskManager(UserSessionService session)
    {
        if (_sessionTaskManagers.Remove(session, out var taskManager))
            taskManager.TaskUpdated -= OnTaskUpdated;
    }

    private async void OnTaskUpdated(object? _, ContentPublishTaskUpdateEventArg e)
    {
        if (e.ProgressEventArg.Status != ContentPublishTaskStatus.Failed)
            return;

        var contentType = MapContentTypeLabel(e.Task.ContentType);
        var title = $"{contentType} \"{e.Task.ContentName}\" Publish failed";

        try
        {
            await desktopNotificationService.SendDesktopNotificationAsync(title, e.ProgressEventArg.ProgressText);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to send desktop notification for publish task {TaskId}",
                e.Task.TaskId);
        }
    }

    private static string MapContentTypeLabel(string contentType)
    {
        return contentType switch
        {
            "world" => "World",
            "avatar" => "Avatar",
            _ => contentType
        };
    }
}

