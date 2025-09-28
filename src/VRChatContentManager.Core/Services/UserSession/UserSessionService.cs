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

    private string _userNameOrEmail;
    private string? _userId;
    private CurrentUser? _currentUser;

    private AsyncServiceScope? _sessionScope;

    internal UserSessionService(
        string userNameOrEmail,
        IServiceScopeFactory scopeFactory,
        string? userId,
        CookieContainer? cookieContainer,
        Func<CookieContainer, string?, string?, Task> saveFunc)
    {
        _scopeFactory = scopeFactory;
        _saveFunc = saveFunc;
        _userId = userId;

        _userNameOrEmail = userNameOrEmail;

        _cookieContainer = cookieContainer ?? new CookieContainer();
        _sessionHttpClient = new HttpClient(
            new InspectorHttpHandler(async () => await _saveFunc(_cookieContainer, _userId, _userNameOrEmail))
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

        _apiClient = new VRChatApiClient(_sessionHttpClient);
    }

    public HttpClient GetHttpClient() => _sessionHttpClient;
    public VRChatApiClient GetApiClient() => _apiClient;

    public async ValueTask<LoginResult> LoginAsync(string password)
    {
        return await _apiClient.LoginAsync(_userNameOrEmail, password);
    }
    
    public async ValueTask<CurrentUser> GetCurrentUserAsync()
    {
        _currentUser = await _apiClient.GetCurrentUser();
        _userId = _currentUser.Id;
        _userNameOrEmail = _currentUser.UserName;

        await _saveFunc(_cookieContainer, _userId, _userNameOrEmail);

        return _currentUser;
    }

    public async Task CreateSessionScopeAsync()
    {
        if (_sessionScope is not null)
            throw new InvalidOperationException("The session scope has already been created.");

        _currentUser = await _apiClient.GetCurrentUser();
        _userId = _currentUser.Id;
        _userNameOrEmail = _currentUser.UserName;
        
        await _saveFunc(_cookieContainer, _userId, _userNameOrEmail);

        await CreateSessionScopeAsyncCore();
    }

    private Task CreateSessionScopeAsyncCore()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var sessionScopeService = scope.ServiceProvider.GetRequiredService<UserSessionScopeService>();
        sessionScopeService.SetUserSessionService(this);

        _sessionScope = scope;

        return Task.CompletedTask;
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

public sealed class UserSessionFactory(IServiceScopeFactory scopeFactory)
{
    public UserSessionService Create(string userNameOrEmail, string? userId, CookieContainer? cookieContainer,
        Func<CookieContainer, string?, string?, Task> saveFunc)
    {
        return new UserSessionService(userNameOrEmail, scopeFactory, userId, cookieContainer, saveFunc);
    }
}