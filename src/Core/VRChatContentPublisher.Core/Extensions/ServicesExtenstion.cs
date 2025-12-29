using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;
using VRChatContentPublisher.ConnectCore.Services.Connect.SessionStorage;
using VRChatContentPublisher.ConnectCore.Services.Health;
using VRChatContentPublisher.ConnectCore.Services.PublishTask;
using VRChatContentPublisher.Core.Services;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.PublishTask.BundleProcesser;
using VRChatContentPublisher.Core.Services.PublishTask.Connect;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;
using VRChatContentPublisher.Core.Services.UserSession;
using VRChatContentPublisher.Core.Services.VRChatApi;
using VRChatContentPublisher.Core.Services.VRChatApi.S3;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddAppCore(this IServiceCollection services)
    {
        services.AddConnectCore();
        services.AddSingleton<ISessionStorageService, RpcClientSessionStorageService>();
        services.AddSingleton<ITokenSecretKeyProvider, RpcTokenSecretKeyProvider>();
        services.AddSingleton<IFileService, TempFileService>();
        services.AddTransient<IConnectMetadataProvider, ConnectMetadataProvider>();

        // Connect Publish Service
        services.AddTransient<IWorldPublishTaskService, WorldPublishTaskService>();
        services.AddTransient<WorldContentPublisherFactory>();

        services.AddTransient<IAvatarPublishTaskService, AvatarPublishTaskService>();
        services.AddTransient<AvatarContentPublisherFactory>();

        services.AddTransient<IHealthService, RpcHealthService>();

        services.AddTransient<BundleCompressProcesser>();

        services.AddMemoryCache();

        services.AddTransient<ConcurrentMultipartUploaderFactory>();

        services.AddSingleton<RemoteImageService>();
        services.AddHttpClient<RemoteImageService>(client => { client.AddUserAgent(); });

        services.AddTransient<VRChatApiClientFactory>();

        services.AddSingleton<UserSessionManagerService>();

        services.AddScoped<UserSessionScopeService>();
        services.AddScoped<TaskManagerService>();

        services.AddTransient<UserSessionFactory>();
        services.AddTransient<ContentPublishTaskFactory>();

        services.AddTransient<AppWebProxy>();
        services.AddTransient<UserSessionHttpClientFactory>();
        // HttpClient only use for upload content to aws s3, DO NOT USE FOR OTHER REQUESTS UNLESS YOU WANT TO LEAK CREDENTIALS
        services.AddHttpClient<ContentPublishTaskFactory>(client =>
            {
                client.AddUserAgent();
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => new SocketsHttpHandler
            {
                UseCookies = false,
                MaxConnectionsPerServer = 256,
                PooledConnectionLifetime = TimeSpan.Zero,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                Proxy = serviceProvider.GetRequiredService<AppWebProxy>()
            })
            .AddResilienceHandler("awsClient", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    UseJitter = true,
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromSeconds(3),
                    BackoffType = DelayBackoffType.Exponential
                });
            });

        return services;
    }

    public static IHostApplicationBuilder UseAppCore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAppCore();

        const string sessionsFileName = "sessions.json";
        builder.Configuration.AddAppJsonFile(sessionsFileName);

        var sessionsSection = builder.Configuration.GetSection("Sessions");
        builder.Services.Configure<UserSessionStorage>(sessionsSection);
        builder.Services.AddWriteableOptions<UserSessionStorage>(sessionsSection.Key, sessionsFileName);

        const string appSettingsFileName = "settings.json";
        builder.Configuration.AddAppJsonFile(appSettingsFileName);

        var appSettingsSection = builder.Configuration.GetSection("Settings");
        builder.Services.Configure<AppSettings>(appSettingsSection);
        builder.Services.AddWriteableOptions<AppSettings>(appSettingsSection.Key, appSettingsFileName);

        const string rpcSessionsFileName = "rpc-sessions.json";
        builder.Configuration.AddAppJsonFile(rpcSessionsFileName);

        var rpcSessionsSection = builder.Configuration.GetSection("RpcSessions");
        builder.Services.Configure<RpcSessionStorage>(rpcSessionsSection);
        builder.Services.AddWriteableOptions<RpcSessionStorage>(rpcSessionsSection.Key, rpcSessionsFileName);

        return builder;
    }

    public static IServiceCollection AddWriteableOptions<T>(this IServiceCollection services, string sectionName,
        string filePath, bool useStoragePath = true)
        where T : class, new()
    {
        services.AddTransient<IWritableOptions<T>>(provider =>
        {
            if (provider.GetRequiredService<IConfiguration>() is not IConfigurationRoot configuration)
                throw new InvalidOperationException("Configuration is not an IConfigurationRoot");

            filePath = useStoragePath ? Path.Combine(AppStorageService.GetStoragePath(), filePath) : filePath;

            var options = provider.GetRequiredService<IOptionsMonitor<T>>();
            var writer = new OptionsWriter(configuration, filePath);

            return new WritableOptions<T>(sectionName, writer, options);
        });

        return services;
    }

    public static IConfigurationManager AddAppJsonFile(this IConfigurationManager configurationManager, string fileName)
    {
        var appSettingsPath = Path.Combine(AppStorageService.GetStoragePath(), fileName);
        configurationManager.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true);
        return configurationManager;
    }
}