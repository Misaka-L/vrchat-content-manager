using VRChatContentManager.ConnectCore.Models.ClientSession;

namespace VRChatContentManager.ConnectCore.Services;

public interface ISessionStorageService
{
    RpcClientSession? GetSessionByClientId(string clientId);
    ValueTask AddSessionAsync(RpcClientSession session);
    ValueTask RemoveSessionByClientIdAsync(string clientId);
    ValueTask RemoveExpiredSessionsAsync();
}