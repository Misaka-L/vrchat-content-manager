using Avalonia.Controls.ApplicationLifetimes;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.Services.AppLifetime;

public sealed class AppLifetimeService : IDisposable
{
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
        if (App.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            throw new NotSupportedException("Only IClassicDesktopStyleApplicationLifetime are supported.");

        desktopLifetime.Shutdown();
    }

    public void Dispose()
    {
        _eventDisposer.Dispose();
    }
}