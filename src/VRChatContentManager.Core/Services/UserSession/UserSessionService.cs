using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
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
        string? userId,
        Func<CookieContainer, string?, string?, Task> saveFunc,
        CookieContainer? cookieContainer,
        VRChatApiClientFactory apiClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<UserSessionService> logger,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _saveFunc = saveFunc;
        UserId = userId;

        UserNameOrEmail = userNameOrEmail;

        _cookieContainer = cookieContainer ?? new CookieContainer();

        var socketHttpHandler = new SocketsHttpHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };

        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            {
                Name = "VRChatApiClient",
                InstanceName = UserId ?? userNameOrEmail
            }
            .AddRetry(new HttpRetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null &&
                    args.Outcome.Exception is not UnexpectedApiBehaviourException &&
                    args.Outcome.Exception is not HttpRequestException &&
                    args.Outcome.Exception is not ApiErrorException),
                UseJitter = true,
                ShouldRetryAfterHeader = true,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Linear
            })
            .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 120,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })
            .ConfigureTelemetry(loggerFactory)
            .Build();

        _sessionHttpClient = new HttpClient(
            new InspectorHttpHandler(async () => await _saveFunc(_cookieContainer, UserId, UserNameOrEmail))
            {
                InnerHandler = new ResilienceHandler(retryPipeline)
                {
                    InnerHandler = new LoggingScopeHttpMessageHandler(logger)
                    {
                        InnerHandler = socketHttpHandler
                    }
                }
            })
        {
            BaseAddress = new Uri("https://api.vrchat.cloud/api/1/"),
            Timeout = TimeSpan.FromSeconds(30)
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

    public async ValueTask LogoutAsync()
    {
        await _apiClient.LogoutAsync();
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

public sealed class UserSessionFactory(
    IServiceScopeFactory scopeFactory,
    VRChatApiClientFactory apiClientFactory,
    ILogger<UserSessionService> logger,
    ILoggerFactory loggerFactory)
{
    public UserSessionService Create(string userNameOrEmail, string? userId, CookieContainer? cookieContainer,
        Func<CookieContainer, string?, string?, Task> saveFunc)
    {
        return new UserSessionService(
            userNameOrEmail,
            userId,
            saveFunc,
            cookieContainer,
            apiClientFactory,
            scopeFactory,
            logger,
            loggerFactory);
    }
}