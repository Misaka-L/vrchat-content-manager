using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VRChatContentManager.Core.Services.App;
using VRChatContentManager.Core.Services.PublishTask;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.Core.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddAppCore(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<RemoteImageService>();
        services.AddHttpClient<RemoteImageService>(client => { client.AddUserAgent(); });
        
        services.AddSingleton<UserSessionManagerService>();

        services.AddScoped<UserSessionScopeService>();
        services.AddScoped<TaskManagerService>();

        services.AddTransient<UserSessionFactory>();
        services.AddTransient<ContentPublishTaskFactory>();

        // HttpClient only use for upload content to aws s3, DO NOT USE FOR OTHER REQUESTS UNLESS YOU WANT TO LEAK CREDENTIALS
        services.AddHttpClient<ContentPublishTaskFactory>(client => { client.AddUserAgent(); });

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