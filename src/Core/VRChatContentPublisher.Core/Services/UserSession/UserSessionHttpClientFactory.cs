using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using VRChatContentPublisher.Core.Resilience;

namespace VRChatContentPublisher.Core.Services.UserSession;

public sealed class UserSessionHttpClientFactory(
    ILoggerFactory loggerFactory,
    AppWebProxy appWebProxy)
{
    public HttpClient Create(
        CookieContainer cookieContainer,
        string instanceName,
        ILogger logger,
        InspectorHttpHandlerDelegate inspector)
    {
        var socketHttpHandler = new SocketsHttpHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            Proxy = appWebProxy
        };

        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            {
                Name = "VRChatApiClient",
                InstanceName = instanceName
            }
            .AddRetry(new AppHttpRetryStrategyOptions
            {
                UseJitter = true,
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 120,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })
            .ConfigureTelemetry(loggerFactory)
            .Build();

        var client = new HttpClient(new ScopeLoggingHttpHandler(logger, instanceName)
        {
            InnerHandler = new InspectorHttpHandler(inspector)
            {
                InnerHandler = new ResilienceHandler(retryPipeline)
                {
                    InnerHandler = new LoggingScopeHttpMessageHandler(logger)
                    {
                        InnerHandler = socketHttpHandler
                    }
                }
            }
        })
        {
            BaseAddress = new Uri("https://api.vrchat.cloud/api/1/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.AddUserAgent();

        return client;
    }
}