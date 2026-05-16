using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VRChatContentPublisher.PersistentCore.Sqlite;

public sealed class SqliteHostedService(
    SqliteDatabaseService sqliteDatabaseService,
    IOptions<SqliteCoreOptions> options
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await sqliteDatabaseService.InitializeAsync(options.Value.DatabasePath);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await sqliteDatabaseService.ShutdownAsync();
    }
}