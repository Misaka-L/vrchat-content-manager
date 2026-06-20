using Avalonia.Controls.ApplicationLifetimes;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.App.Services.AppLifetime;

public sealed class AppLifetimeService : IDisposable
{
    public bool IsShutdownRequested { get; private set; }
    
    private readonly TaskCompletionSource _hostStartedTcs = new();

    public event EventHandler<bool>? IsSafeToShutdownChanged;

    private readonly UserSessionManagerService _userSessionManagerService;
    private readonly IDisposable _eventDisposer;

    public AppLifetimeService(UserSessionManagerService userSessionManagerService,
        ISubscriber<ContentPublishTaskCreatedEventArg> taskCreatedSubscriber,
        ISubscriber<ContentPublishTaskRemovedEventArg> taskRemovedSubscriber,
        ISubscriber<ContentPublishTaskUpdateEventArg> taskUpdateSubscriber
    )
    {
        _userSessionManagerService = userSessionManagerService;
        _eventDisposer = DisposableBag.Create(
            taskCreatedSubscriber.Subscribe(_ => NotifyIsSafeToShutdownChanged()),
            taskRemovedSubscriber.Subscribe(_ => NotifyIsSafeToShutdownChanged()),
            taskUpdateSubscriber.Subscribe(_ => NotifyIsSafeToShutdownChanged())
        );
    }

    internal void NotifyHostStartError(Exception ex)
    {
        _hostStartedTcs.TrySetException(ex);
    }

    internal void NotifyHostStarted()
    {
        _hostStartedTcs.TrySetResult();
    }

    internal Task WaitForHostStartedAsync() => _hostStartedTcs.Task;

    private void NotifyIsSafeToShutdownChanged()
    {
        IsSafeToShutdownChanged?.Invoke(this, IsSafeToShutdown());
    }

    public bool IsSafeToShutdown()
    {
        foreach (var session in _userSessionManagerService.Sessions)
        {
            if (session.TryGetSessionScope() is not { } scope) continue;

            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            if (taskManager.Tasks.Any(t => t.Value.Status != ContentPublishTaskStatus.Completed))
                return false;
        }

        return true;
    }

    public void Shutdown()
    {
        if (IsShutdownRequested)
            return;

        if (App.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            throw new NotSupportedException("Only IClassicDesktopStyleApplicationLifetime are supported.");

        IsShutdownRequested = true;
        desktopLifetime.Shutdown();
    }

    public void Dispose()
    {
        _eventDisposer.Dispose();
    }
}