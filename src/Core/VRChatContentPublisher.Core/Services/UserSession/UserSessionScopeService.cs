namespace VRChatContentPublisher.Core.Services.UserSession;

public sealed class UserSessionScopeService
{
    private UserSessionService? _userSessionService;
    
    internal void SetUserSessionService(UserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
    }
}