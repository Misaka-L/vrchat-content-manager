using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.Services;

public sealed class AppLifetimeService(UserSessionManagerService userSessionManagerService)
{
    public async ValueTask<bool> IsSafeToShutdownAsync()
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            if (!session.IsScopeInitialized) continue;

            var scope = await session.CreateOrGetSessionScopeAsync();
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
}