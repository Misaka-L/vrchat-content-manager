using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentPublisher.Core.Services.VRChatApi;

namespace VRChatContentPublisher.Core.Services.UserSession;

public sealed class UserSessionService : IAsyncDisposable, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserSessionService> _logger;

    // Cookies, UserId, UserName
    private readonly SaveCookiesDelegate _saveFunc;

    private readonly HttpClient _sessionHttpClient;
    private readonly VRChatApiClient _apiClient;

    public CookieContainer CookieContainer { get; }

    public event EventHandler<UserSessionState>? StateChanged;
    public event EventHandler<CurrentUser?>? CurrentUserUpdated;
    public UserSessionState State { get; set; } = UserSessionState.Pending;

    public string UserNameOrEmail { get; private set; }
    public string? UserId { get; private set; }
    public CurrentUser? CurrentUser { get; private set; }

    private AsyncServiceScope? _sessionScope;

    internal UserSessionService(
        string userNameOrEmail,
        string? userId,
        CurrentUser user,
        SaveCookiesDelegate saveFunc,
        CookieContainer? cookieContainer,
        VRChatApiClientFactory apiClientFactory,
        IServiceScopeFactory scopeFactory,
        UserSessionHttpClientFactory httpClientFactory,
        ILogger<UserSessionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _saveFunc = saveFunc;
        UserId = userId;
        CurrentUser = user;

        UserNameOrEmail = userNameOrEmail;

        CookieContainer = cookieContainer ?? new CookieContainer();

        if (cookieContainer is null || userId is null)
        {
            OnStateChanged(UserSessionState.LoggedOut);
        }

        _sessionHttpClient = httpClientFactory.Create(
            CookieContainer,
            userId ?? userNameOrEmail,
            logger,
            AfterHttpResponseAsync
        );

        _apiClient = apiClientFactory.Create(_sessionHttpClient);
    }

    public HttpClient GetHttpClient() => _sessionHttpClient;
    public VRChatApiClient GetApiClient() => _apiClient;

    public async ValueTask<LoginResult> LoginAsync(string password)
    {
        var result = await _apiClient.LoginAsync(UserNameOrEmail, password);
        OnStateChanged(result.IsSuccess ? UserSessionState.LoggedIn : UserSessionState.LoggedOut);

        return result;
    }

    public async ValueTask LogoutAsync()
    {
        OnStateChanged(UserSessionState.LoggedOut);
        await _apiClient.LogoutAsync();
    }

    public async ValueTask<bool> TryRepairAsync()
    {
        try
        {
            await GetCurrentUserAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to auto repair (check is session really invalid) user session for {UserNameOrEmail}",
                UserNameOrEmail);
            return false;
        }

        return true;
    }

    public async ValueTask<CurrentUser> GetCurrentUserAsync()
    {
        try
        {
            CurrentUser = await _apiClient.GetCurrentUser();
            UserId = CurrentUser.Id;
            UserNameOrEmail = CurrentUser.UserName;
        }
        catch (ApiErrorException ex) when (ex.StatusCode == 401)
        {
            OnStateChanged(UserSessionState.InvalidSession);
            throw;
        }

        await _saveFunc(CookieContainer, UserId, UserNameOrEmail, CurrentUser);
        OnStateChanged(UserSessionState.LoggedIn);

        CurrentUserUpdated?.Invoke(this, CurrentUser);
        return CurrentUser;
    }

    public async ValueTask<AsyncServiceScope> CreateOrGetSessionScopeAsync()
    {
        if (_sessionScope is { } scope)
            return scope;

        CurrentUser = await GetCurrentUserAsync();

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

    private async Task AfterHttpResponseAsync(HttpResponseMessage response)
    {
        await _saveFunc(CookieContainer, UserId, UserNameOrEmail, CurrentUser);

        if (State == UserSessionState.LoggedIn &&
            response.RequestMessage?.RequestUri?.Host == "api.vrchat.cloud" &&
            response.StatusCode == HttpStatusCode.Unauthorized)
        {
            OnStateChanged(UserSessionState.InvalidSession);
        }
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

    private void OnStateChanged(UserSessionState e)
    {
        if (e == State)
            return;

        State = e;
        StateChanged?.Invoke(this, e);
    }
}

public sealed class UserSessionFactory(
    IServiceScopeFactory scopeFactory,
    VRChatApiClientFactory apiClientFactory,
    UserSessionHttpClientFactory httpClientFactory,
    ILogger<UserSessionService> logger)
{
    public UserSessionService Create(
        string userNameOrEmail,
        string? userId,
        CookieContainer? cookieContainer,
        CurrentUser? user,
        SaveCookiesDelegate saveFunc
    )
    {
        return new UserSessionService(
            userNameOrEmail,
            userId,
            user,
            saveFunc,
            cookieContainer,
            apiClientFactory,
            scopeFactory,
            httpClientFactory,
            logger);
    }
}

public enum UserSessionState
{
    Pending,
    LoggedOut,
    LoggedIn,
    InvalidSession
}

public delegate Task SaveCookiesDelegate(
    CookieContainer cookies,
    string? userId,
    string? userNameOrEmail,
    CurrentUser? user);