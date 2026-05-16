using Microsoft.Extensions.Hosting;

namespace VRChatContentPublisher.Core.Database;

public class TableInitializationHostedService(
    ContentPublishTaskDatabaseService contentPublishTaskDatabaseService,
    FileDatabaseService fileDatabaseService
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await contentPublishTaskDatabaseService.InitializeAsync();
        await fileDatabaseService.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}