using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckBackgroundService(
    ILogger<AppUpdateCheckBackgroundService> logger,
    IWritableOptions<AppSettings> appSettings,
    AppUpdateCheckService updateCheckService
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (appSettings.Value.UpdateCheckMode != AppUpdateCheckMode.Manual)
        {
            try
            {
                await updateCheckService.CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while checking for updates at startup");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                logger.LogInformation("Checking for updates in background...");
                await updateCheckService.CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while checking for updates in background");
            }
        }
    }
}