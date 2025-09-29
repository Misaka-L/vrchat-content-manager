using System.Net;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.Core.Services.UserSession;

public sealed class UserSessionManagerService(
    UserSessionFactory sessionFactory,
    IWritableOptions<UserSessionStorage> userSessionStorage)
{
    private readonly List<UserSessionService> _sessions = [];
    public IReadOnlyList<UserSessionService> Sessions => _sessions.AsReadOnly();

    public async Task RestoreSessionsAsync()
    {
        foreach (var (userId, sessionItem) in userSessionStorage.Value.Sessions)
        {
            var cookieContainer = new CookieContainer();
            foreach (var cookie in sessionItem.Cookies)
            {
                cookieContainer.Add(cookie);
            }

            var session = CreateOrGetSession(sessionItem.UserName, userId, cookieContainer);
            try
            {
                await session.CreateSessionScopeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public UserSessionService CreateOrGetSession(string userNameOrEmail, string? userId = null, CookieContainer?
        cookieContainer = null)
    {
        if (_sessions.FirstOrDefault(session =>
                (session.UserId is not null && userId == session.UserId) ||
                session.UserNameOrEmail == userNameOrEmail
            ) is { } existingSession)
        {
            return existingSession;
        }

        var session = sessionFactory.Create(userNameOrEmail, userId, cookieContainer,
            async (cookies, sessionUserId, userName) =>
            {
                if (sessionUserId is null || userName is null)
                    return;

                await userSessionStorage.UpdateAsync(sessions =>
                {
                    if (sessions.Sessions.TryGetValue(sessionUserId, out var session))
                    {
                        session.Cookies.Clear();
                        session.Cookies.AddRange(cookies.GetAllCookies());
                        return;
                    }

                    sessions.Sessions.Add(sessionUserId,
                        new UserSessionStorageItem(userName, new List<Cookie>(cookies.GetAllCookies())));
                });
            });

        _sessions.Add(session);

        return session;
    }
}