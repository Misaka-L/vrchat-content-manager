using VRChatContentManager.ConnectCore.Services.Health;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services;

public sealed class RpcHealthService(UserSessionManagerService userSessionManagerService) : IHealthService
{
    public ValueTask<bool> IsReadyForPublishAsync()
    {
        return
            ValueTask.FromResult(
                userSessionManagerService.Sessions.Count > 0 &&
                userSessionManagerService.Sessions.All(s => s.State == UserSessionState.LoggedIn)
            );
    }
}