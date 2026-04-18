using Avalonia.Threading;
using DialogHostAvalonia;
using Microsoft.Extensions.Hosting;

namespace VRChatContentPublisher.App.Services.Dialog;

public sealed class DialogBackgroundService(DialogService dialogService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await dialogService.DialogHostReadTask;

        var reader = dialogService.DialogChannel.Reader;
        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await reader.ReadAsync(stoppingToken);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var result = await DialogHost.Show(item.Dialog, dialogService.DialogHostId);
                item.TaskCompletionSource.SetResult(result);
            }, DispatcherPriority.Default, stoppingToken);
        }
    }
}