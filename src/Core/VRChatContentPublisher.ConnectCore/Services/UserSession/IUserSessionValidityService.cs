namespace VRChatContentPublisher.ConnectCore.Services.UserSession;

public interface IUserSessionValidityService
{
    /// <summary>
    /// Re-requests the current user from the VRChat API (<c>/auth/user</c>) for the session that belongs to
    /// <paramref name="userId"/> and reports whether that session is still valid. The session state (including
    /// whether it becomes invalid) is handled by the user session service based on the response, so no explicit
    /// result handling is required by the caller.
    /// </summary>
    ValueTask<UserSessionValidityStatus> CheckSessionValidityAsync(string userId);
}

public enum UserSessionValidityStatus
{
    /// <summary>The session exists and the VRChat API still accepts it.</summary>
    Valid,

    /// <summary>The session exists but requesting the current user failed.</summary>
    Invalid,

    /// <summary>No user session for the requested VRChat user ID exists in the app.</summary>
    SessionNotFound
}