using VRChatContentPublisher.ConnectCore.Services.Health;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.Core.Services;

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