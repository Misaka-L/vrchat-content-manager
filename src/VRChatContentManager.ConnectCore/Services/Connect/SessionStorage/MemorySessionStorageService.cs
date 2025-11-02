using VRChatContentManager.ConnectCore.Models.ClientSession;

namespace VRChatContentManager.ConnectCore.Services.Connect.SessionStorage;

public sealed class MemorySessionStorageService : ISessionStorageService
{
    private readonly Lock _sessionLock = new();

    private readonly List<RpcClientSession> _sessions = [];
    
    private string _sessionIssuer = Guid.NewGuid().ToString("D");

    public RpcClientSession? GetSessionByClientId(string clientId)
    {
        lock (_sessionLock)
        {
            return _sessions.Find(session => session.ClientId == clientId);
        }
    }

    public ValueTask AddSessionAsync(RpcClientSession session)
    {
        lock (_sessionLock)
        {
            _sessions.Add(session);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveSessionByClientIdAsync(string clientId)
    {
        lock (_sessionLock)
        {
            _sessions.RemoveAll(session => session.ClientId == clientId);
        }

        return ValueTask.CompletedTask;
    }
    
    public ValueTask RemoveExpiredSessionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        lock (_sessionLock)
        {
            _sessions.RemoveAll(session => session.Expires <= now);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<string> GetIssuerAsync() => ValueTask.FromResult(_sessionIssuer);
}