using VRChatContentManager.ConnectCore.Models.ClientSession;
using VRChatContentManager.ConnectCore.Services.Connect.SessionStorage;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.Core.Services;

public sealed class RpcClientSessionStorageService(IWritableOptions<RpcSessionStorage> sessionsStorage) : ISessionStorageService
{
    public RpcClientSession? GetSessionByClientId(string clientId)
    {
        return sessionsStorage.Value.Sessions.Find(session => session.ClientId == clientId);
    }

    public async ValueTask AddSessionAsync(RpcClientSession session)
    {
        await sessionsStorage.UpdateAsync(sessions =>
        {
            sessions.Sessions.Add(session);
        });
    }

    public async ValueTask RemoveSessionByClientIdAsync(string clientId)
    {
        await sessionsStorage.UpdateAsync(sessions =>
        {
            sessions.Sessions.RemoveAll(session => session.ClientId == clientId);
        });
    }

    public async ValueTask RemoveExpiredSessionsAsync()
    {
        await sessionsStorage.UpdateAsync(sessions =>
        {
            var now = DateTimeOffset.UtcNow;
            sessions.Sessions.RemoveAll(session => session.Expires < now);
        });
    }
}