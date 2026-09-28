using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Services.UserSession;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.Core.Rpc;

public sealed class UserSessionValidityService(
    UserSessionManagerService userSessionManagerService,
    ILogger<UserSessionValidityService> logger) : IUserSessionValidityService
{
    public async ValueTask<UserSessionValidityStatus> CheckSessionValidityAsync(string userId)
    {
        if (userSessionManagerService.Sessions.FirstOrDefault(s => s.UserId == userId) is not { } session)
        {
            logger.LogInformation("RPC client asked for the session validity of unknown VRChat user {UserId}", userId);
            return UserSessionValidityStatus.SessionNotFound;
        }

        // Requests /auth/user and lets the session service update its own state (InvalidSession on 401,
        // LoggedIn on success), so any failure is reported to the RPC client as an invalid session.
        return await session.RefreshSessionStateAsync()
            ? UserSessionValidityStatus.Valid
            : UserSessionValidityStatus.Invalid;
    }
}