using System.Net;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentManager.Core.Services.VRChatApi;

namespace VRChatContentManager.Core.Services.UserSession;

public sealed class UserSessionService : IAsyncDisposable, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Cookies, UserId, UserName
    private readonly Func<CookieContainer, string?, string?, Task> _saveFunc;

    private readonly HttpClient _sessionHttpClient;
    private readonly CookieContainer _cookieContainer;

    private readonly VRChatApiClient _apiClient;

    public string UserNameOrEmail { get; private set; }
    public string? UserId { get; private set; }
    public CurrentUser? CurrentUser { get; private set; }

    private AsyncServiceScope? _sessionScope;

    internal UserSessionService(
        string userNameOrEmail,
        IServiceScopeFactory scopeFactory,
        string? userId,
        CookieContainer? cookieContainer,
        VRChatApiClientFactory apiClientFactory,
        Func<CookieContainer, string?, string?, Task> saveFunc)
    {
        _scopeFactory = scopeFactory;
        _saveFunc = saveFunc;
        UserId = userId;

        UserNameOrEmail = userNameOrEmail;

        _cookieContainer = cookieContainer ?? new CookieContainer();
        _sessionHttpClient = new HttpClient(
            new InspectorHttpHandler(async () => await _saveFunc(_cookieContainer, UserId, UserNameOrEmail))
            {
                InnerHandler = new SocketsHttpHandler
                {
                    CookieContainer = _cookieContainer,
                    UseCookies = true
                }
            })
        {
            BaseAddress = new Uri("https://api.vrchat.cloud/api/1/")
        };

        _sessionHttpClient.AddUserAgent();

        _apiClient = apiClientFactory.Create(_sessionHttpClient);
    }

    public HttpClient GetHttpClient() => _sessionHttpClient;
    public VRChatApiClient GetApiClient() => _apiClient;

    public async ValueTask<LoginResult> LoginAsync(string password)
    {
        return await _apiClient.LoginAsync(UserNameOrEmail, password);
    }

    public async ValueTask<CurrentUser> GetCurrentUserAsync()
    {
        CurrentUser = await _apiClient.GetCurrentUser();
        UserId = CurrentUser.Id;
        UserNameOrEmail = CurrentUser.UserName;

        await _saveFunc(_cookieContainer, UserId, UserNameOrEmail);

        return CurrentUser;
    }

    public async ValueTask<AsyncServiceScope> CreateOrGetSessionScopeAsync()
    {
        if (_sessionScope is { } scope)
            return scope;

        CurrentUser = await _apiClient.GetCurrentUser();
        UserId = CurrentUser.Id;
        UserNameOrEmail = CurrentUser.UserName;

        await _saveFunc(_cookieContainer, UserId, UserNameOrEmail);

        return await CreateSessionScopeAsyncCore();
    }

    private ValueTask<AsyncServiceScope> CreateSessionScopeAsyncCore()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var sessionScopeService = scope.ServiceProvider.GetRequiredService<UserSessionScopeService>();
        sessionScopeService.SetUserSessionService(this);

        _sessionScope = scope;

        return ValueTask.FromResult(scope);
    }

    #region Dispose

    public ValueTask DisposeAsync()
    {
        _sessionHttpClient.Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _sessionHttpClient.Dispose();
    }

    #endregion
}

public sealed class UserSessionFactory(IServiceScopeFactory scopeFactory, VRChatApiClientFactory apiClientFactory)
{
    public UserSessionService Create(string userNameOrEmail, string? userId, CookieContainer? cookieContainer,
        Func<CookieContainer, string?, string?, Task> saveFunc)
    {
        return new UserSessionService(userNameOrEmail, scopeFactory, userId, cookieContainer, apiClientFactory, saveFunc);
    }
}