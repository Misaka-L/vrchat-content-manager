using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;
using VRChatContentPublisher.ConnectCore.Services.Connect.SessionStorage;
using VRChatContentPublisher.ConnectCore.Services.Health;
using VRChatContentPublisher.ConnectCore.Services.PublishTask;
using VRChatContentPublisher.Core.Resilience;
using VRChatContentPublisher.Core.Services;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Services.PublicIp;
using VRChatContentPublisher.Core.Services.PublishTask;
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
        services.ConfigureHttpClientDefaults(builder =>
        {
            var clientName = string.IsNullOrWhiteSpace(builder.Name) ? "general" : builder.Name + "-general";

            builder
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.AddUserAgent();
                })
                .ConfigurePrimaryHttpMessageHandler(servicesProvider => new SocketsHttpHandler
                {
                    UseCookies = false,
                    PooledConnectionLifetime = TimeSpan.Zero,
                    EnableMultipleHttp2Connections = true,
                    EnableMultipleHttp3Connections = true,
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    Proxy = servicesProvider.GetRequiredService<AppWebProxy>()
                })
                .AddResilienceHandler(clientName, handlerBuilder =>
                {
                    handlerBuilder
                        .AddRetry(new AppHttpRetryStrategyOptions
                        {
                            UseJitter = true,
                            MaxRetryAttempts = 5,
                            Delay = TimeSpan.FromSeconds(5),
                            BackoffType = DelayBackoffType.Exponential
                        })
                        .AddTimeout(new HttpTimeoutStrategyOptions
                        {
                            Timeout = TimeSpan.FromSeconds(30)
                        });
                });
        });

        services.AddConnectCore();
        services.AddHostedService<RpcServerStartupHostedService>();
        services.AddSingleton<RpcStartupPortWarningState>();
        services.AddSingleton<ISessionStorageService, RpcClientSessionStorageService>();
        services.AddSingleton<ITokenSecretKeyProvider, RpcTokenSecretKeyProvider>();
        services.AddSingleton<IFileService, TempFileService>();
        services.AddTransient<IConnectMetadataProvider, ConnectMetadataProvider>();

        // Connect Publish Service
        services.AddTransient<IWorldPublishTaskService, WorldPublishTaskService>();
        services.AddTransient<WorldContentPublisherFactory>();

        services.AddTransient<IAvatarPublishTaskService, AvatarPublishTaskService>();
        services.AddTransient<AvatarContentPublisherFactory>();

        services.AddTransient<BundleProcessService>(_ => new BundleProcessService(new BundleProcessPipelineOptions
        {
            TempFolderPath = Path.Combine(AppStorageService.GetTempPath(), "bundle-process-temp"),
        }));

        services.AddTransient<IHealthService, RpcHealthService>();

        services.AddMemoryCache();

        services.AddTransient<ConcurrentMultipartUploaderFactory>();

        services.AddSingleton<RemoteImageService>();

        services.AddTransient<VRChatApiClientFactory>();

        services.AddSingleton<UserSessionManagerService>();

        services.AddScoped<UserSessionScopeService>();
        services.AddScoped<TaskManagerService>();

        services.AddTransient<UserSessionFactory>();
        services.AddTransient<ContentPublishTaskFactory>();

        services.AddTransient<AppWebProxy>();
        services.AddTransient<UserSessionHttpClientFactory>();

        // HttpClient only use for upload content to aws s3, DO NOT USE FOR OTHER REQUESTS UNLESS YOU WANT TO LEAK CREDENTIALS
#pragma warning disable EXTEXP0001
        services.AddHttpClient<ContentPublishTaskFactory>()
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
            .RemoveAllResilienceHandlers()
            .AddResilienceHandler("awsClient", builder =>
            {
                builder.AddRetry(new AppHttpRetryStrategyOptions
                {
                    UseJitter = true,
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromSeconds(3),
                    BackoffType = DelayBackoffType.Exponential
                });
            });
#pragma warning restore EXTEXP0001

        services.AddTransient<VRChatApiDiagnosticService>();

        services.AddSingleton<IIpCryptService, IpCryptService>();
        services.AddSingleton<PublicIpCheckerService>();
        services.AddSingleton<CloudflareTracePublicIpProvider>();

        services.AddHostedService<PublicIpMonitorBackgroundService>();

        services.AddMessagePipe();

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

        const string publicIpStateFileName = "public-ip-state.json";
        builder.Configuration.AddAppJsonFile(publicIpStateFileName);

        var publicIpStateSection = builder.Configuration.GetSection("PublicIpState");
        builder.Services.Configure<PublicIpStateStorage>(publicIpStateSection);
        builder.Services.AddWriteableOptions<PublicIpStateStorage>(publicIpStateSection.Key, publicIpStateFileName);

        const string ipCryptFileName = "ipcrypt.json";
        builder.Configuration.AddAppJsonFile(ipCryptFileName);

        var ipCryptSection = builder.Configuration.GetSection("IpCrypt");
        builder.Services.Configure<IpCryptStorage>(ipCryptSection);
        builder.Services.AddWriteableOptions<IpCryptStorage>(ipCryptSection.Key, ipCryptFileName);

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