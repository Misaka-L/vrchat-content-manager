using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.PersistentCore.Sqlite;

namespace VRChatContentPublisher.PersistentCore.Extensions;

public static class ServiceExtension
{
    public static IServiceCollection AddSqliteCore(
        this IServiceCollection services,
        Action<SqliteCoreOptions> configure
    )
    {
        services.Configure(configure);

        services.AddSingleton<SqliteDatabaseService>();
        services.AddHostedService<SqliteHostedService>();

        return services;
    }
}