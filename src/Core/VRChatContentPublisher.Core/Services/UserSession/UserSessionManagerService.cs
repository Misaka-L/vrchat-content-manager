using System.Net;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.UserSession;

public sealed class UserSessionManagerService(
    UserSessionFactory sessionFactory,
    IWritableOptions<UserSessionStorage> userSessionStorage,
    ILogger<UserSessionManagerService> logger)
{
    private readonly List<UserSessionService> _sessions = [];
    private UserSessionService? _defaultSession;
    public IReadOnlyList<UserSessionService> Sessions => _sessions.AsReadOnly();

    public event EventHandler<UserSessionService>? SessionCreated;
    public event EventHandler<UserSessionService>? SessionRemoved;
    public event EventHandler<UserSessionService?>? DefaultSessionChanged;

    public bool IsAnySessionAvailable =>
        _sessions.Count > 0 && _sessions.Any(session => session.State == UserSessionState.LoggedIn);

    public async Task RestoreSessionsAsync(Action<UserSessionService, Exception>? onSessionRestoredFailed = null)
    {
        foreach (var (userId, sessionItem) in userSessionStorage.Value.Sessions)
        {
            var cookieContainer = new CookieContainer();
            if (sessionItem.Cookies != null)
                foreach (var cookie in sessionItem.Cookies)
                {
                    cookieContainer.Add(cookie);
                }

            var session = CreateOrGetSession(sessionItem.UserName, userId, cookieContainer, sessionItem.GetApiUserModel());
            try
            {
                await session.CreateOrGetSessionScopeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to restore session for user ({UserId}) {UserName}", userId,
                    sessionItem.UserName);
                onSessionRestoredFailed?.Invoke(session, ex);
            }
        }

        var resolvedDefaultSession = ResolveDefaultSessionFromSettings();
        _defaultSession = resolvedDefaultSession;

        if (resolvedDefaultSession is not null)
            return;

        var hasDefaultSelection =
            !string.IsNullOrWhiteSpace(userSessionStorage.Value.DefaultAccountUserId) ||
            !string.IsNullOrWhiteSpace(userSessionStorage.Value.DefaultAccountUserNameOrEmail);
        if (!hasDefaultSelection)
            return;

        await ClearDefaultSessionStorageAsync();
    }

    public UserSessionService? GetDefaultSession()
    {
        if (_defaultSession is not null && _sessions.Contains(_defaultSession))
            return _defaultSession;

        _defaultSession = ResolveDefaultSessionFromSettings();
        return _defaultSession;
    }

    public bool IsDefaultSession(UserSessionService session)
    {
        return GetDefaultSession() == session;
    }

    public async ValueTask SetDefaultSessionAsync(UserSessionService session)
    {
        if (!_sessions.Contains(session))
            throw new InvalidOperationException("Session does not exist.");

        var existingDefaultSession = GetDefaultSession();
        if (existingDefaultSession == session)
            return;

        await userSessionStorage.UpdateAsync(storage =>
        {
            storage.DefaultAccountUserId = session.UserId;
            storage.DefaultAccountUserNameOrEmail = session.UserNameOrEmail;
        });

        _defaultSession = session;
        OnDefaultSessionChanged(session);
    }

    public async ValueTask ClearDefaultSessionAsync()
    {
        if (_defaultSession is null &&
            string.IsNullOrWhiteSpace(userSessionStorage.Value.DefaultAccountUserId) &&
            string.IsNullOrWhiteSpace(userSessionStorage.Value.DefaultAccountUserNameOrEmail))
        {
            return;
        }

        await ClearDefaultSessionStorageAsync();
        _defaultSession = null;
        OnDefaultSessionChanged(null);
    }

    public bool IsSessionExists(string userNameOrEmail)
    {
        return _sessions.Any(session =>
            session.UserNameOrEmail == userNameOrEmail ||
            session.CurrentUser?.UserName == userNameOrEmail
        );
    }

    public UserSessionService CreateOrGetSession(string userNameOrEmail, string? userId = null, CookieContainer?
        cookieContainer = null, CurrentUser? user = null)
    {
        if (_sessions.FirstOrDefault(session =>
                (session.UserId is not null && userId == session.UserId) ||
                session.UserNameOrEmail == userNameOrEmail
            ) is { } existingSession)
        {
            return existingSession;
        }

        var session = sessionFactory.Create(userNameOrEmail, userId, cookieContainer, user,
            async (cookies, sessionUserId, userName, userInfo) =>
            {
                if (sessionUserId is null || userName is null)
                    return;

                await userSessionStorage.UpdateAsync(sessions =>
                {
                    if (sessions.Sessions.TryGetValue(sessionUserId, out var session))
                    {
                        session.Cookies?.Clear();
                        session.Cookies?.AddRange(cookies.GetAllCookies());

                        if (userInfo is not null)
                            session.User = UserSessionUserInfo.Create(userInfo);
                        return;
                    }

                    sessions.Sessions.Add(sessionUserId,
                        new UserSessionStorageItem(
                            userName,
                            new List<Cookie>(cookies.GetAllCookies()),
                            userInfo != null ? UserSessionUserInfo.Create(userInfo) : null));
                });
            });

        _sessions.Add(session);
        OnSessionCreated(session);

        return session;
    }

    public async ValueTask<UserSessionService> HandleSessionAfterLogin(UserSessionService session)
    {
        if (Sessions.All(s => s != session))
            throw new InvalidOperationException("Session no exists in manager.");

        var user = await session.GetCurrentUserAsync();
        // Replace existing session if try login with same user
        if (Sessions.FirstOrDefault(existSession =>
                existSession != session && existSession.UserId == user.Id)
            is not { } existingSession)
            return session;

        logger.LogWarning("Replacing existing session for user ({UserId}) {UserName}", user.Id, user.UserName);
        logger.LogInformation("Logging out existing session for user ({UserId}) {UserName}", existingSession.UserId,
            existingSession.UserNameOrEmail);
        try
        {
            await existingSession.LogoutAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to logout existing session for user ({UserId}) {UserName}",
                existingSession.UserId, existingSession.UserNameOrEmail);
        }

        logger.LogInformation("Transferring cookies to existing session for user ({UserId}) {UserName}",
            existingSession.UserId, existingSession.UserNameOrEmail);
        foreach (Cookie cookie in existingSession.CookieContainer.GetAllCookies())
        {
            cookie.Expired = true;
        }

        var cookies = session.CookieContainer.GetAllCookies();
        foreach (Cookie cookie in cookies)
        {
            existingSession.CookieContainer.Add(cookie);
        }

        logger.LogInformation("Refreshing existing session for user ({UserId}) {UserName}", existingSession.UserId,
            existingSession.UserNameOrEmail);
        await existingSession.GetCurrentUserAsync();

        logger.LogInformation("Removing temporary session for user ({UserId}) {UserName}", session.UserId,
            session.UserNameOrEmail);
        await RemoveSessionAsync(session, false);

        return existingSession;
    }

    public async ValueTask RemoveSessionAsync(UserSessionService session, bool logout = true)
    {
        logger.LogInformation("Removing session for user ({UserId}) {UserName}", session.UserId,
            session.UserNameOrEmail);

        if (!_sessions.Contains(session))
        {
            logger.LogError("Tried to remove a session that does not exist for user ({UserId}) {UserName}",
                session.UserId, session.UserNameOrEmail);
            throw new InvalidOperationException("Session does not exist.");
        }

        var isDefaultSession = IsDefaultSession(session);

        _sessions.Remove(session);
        OnSessionRemoved(session);

        if (isDefaultSession)
            await ClearDefaultSessionAsync();

        if (logout)
        {
            logger.LogInformation("Logging out session for user ({UserId}) {UserName}", session.UserId,
                session.UserNameOrEmail);
            try
            {
                await session.LogoutAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to logout session for user ({UserId}) {UserName}", session.UserId,
                    session.UserNameOrEmail);
            }
        }

        await session.DisposeAsync();

        await userSessionStorage.UpdateAsync(storage =>
        {
            if (session.UserId is { } userId)
                storage.Sessions.Remove(userId);
        });

        logger.LogInformation("Session removed for user ({UserId}) {UserName}", session.UserId,
            session.UserNameOrEmail);
    }

    private void OnSessionCreated(UserSessionService e)
    {
        SessionCreated?.Invoke(this, e);
    }

    private void OnSessionRemoved(UserSessionService e)
    {
        SessionRemoved?.Invoke(this, e);
    }

    private UserSessionService? ResolveDefaultSessionFromSettings()
    {
        var defaultUserId = userSessionStorage.Value.DefaultAccountUserId;
        if (!string.IsNullOrWhiteSpace(defaultUserId) &&
            _sessions.FirstOrDefault(session => session.UserId == defaultUserId) is { } sessionByUserId)
        {
            return sessionByUserId;
        }

        var defaultUserNameOrEmail = userSessionStorage.Value.DefaultAccountUserNameOrEmail;
        if (!string.IsNullOrWhiteSpace(defaultUserNameOrEmail) &&
            _sessions.FirstOrDefault(session => session.UserNameOrEmail == defaultUserNameOrEmail) is { } sessionByUserName)
        {
            return sessionByUserName;
        }

        return null;
    }

    private async ValueTask ClearDefaultSessionStorageAsync()
    {
        await userSessionStorage.UpdateAsync(storage =>
        {
            storage.DefaultAccountUserId = null;
            storage.DefaultAccountUserNameOrEmail = null;
        });
    }

    private void OnDefaultSessionChanged(UserSessionService? session)
    {
        DefaultSessionChanged?.Invoke(this, session);
    }
}