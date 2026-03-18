using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.Core.Events.UserSession;

public sealed class SessionStateChangedEvent(string? userId, string userNameOrEmail, UserSessionState sessionState)
{
    public string? UserId => userId;
    public string UserNameOrEmail => userNameOrEmail;

    public UserSessionState SessionState => sessionState;
}