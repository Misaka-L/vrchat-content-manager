using VRChatContentManager.ConnectCore.Models.ClientSession;

namespace VRChatContentManager.ConnectCore.Services.Connect.SessionStorage;

public interface ISessionStorageService
{
    RpcClientSession? GetSessionByClientId(string clientId);
    ValueTask AddSessionAsync(RpcClientSession session);
    ValueTask RemoveSessionByClientIdAsync(string clientId);
    ValueTask RemoveExpiredSessionsAsync();

    ValueTask<string> GetIssuerAsync();
}