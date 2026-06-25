using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.App.Services.AppLifetime;

public sealed class AppLifetimeService : IDisposable
{
    public event EventHandler<bool>? IsSafeToShutdownChanged;

    private readonly TaskCompletionSource _hostStartedTcs = new();
    private readonly TaskCompletionSource _appStartupTcs = new();

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

    internal void NotifyAppStartup()
    {
        _appStartupTcs.TrySetResult();
    }

    internal Task WaitForHostStartedAsync() => _hostStartedTcs.Task;

    internal async Task WaitAppStartupNotifyHostShutdown(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        try
        {
            await _appStartupTcs.Task.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;

        PostShutdown();
    }

    private void PostShutdown()
    {
        try
        {
            Dispatcher.UIThread.Post(Shutdown, DispatcherPriority.MaxValue);
        }
        catch (Exception ex)
        {
            Log.Error("Dispatcher.UIThread.Post(Shutdown) failed", ex);
        }
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

    /// <summary>
    /// Ensure that shutdown is not called multiple times. Use only on the UIThread.
    /// </summary>
    private bool _isShutdownRequested;

    /// <summary>
    /// Force Shutdown App
    /// </summary>
    /// <exception cref="InvalidOperationException">Called from a thread other than the UIThread</exception>
    /// <exception cref="NotSupportedException">Only IClassicDesktopStyleApplicationLifetime are supported</exception>
    public void Shutdown()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new InvalidOperationException("Can only be called from the UIThread.");

        if (_isShutdownRequested)
            return;

        if (App.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            throw new NotSupportedException("Only IClassicDesktopStyleApplicationLifetime are supported.");

        _isShutdownRequested = true;
        desktopLifetime.Shutdown();
    }

    public void Dispose()
    {
        _eventDisposer.Dispose();
    }
}