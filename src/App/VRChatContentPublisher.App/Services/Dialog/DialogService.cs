using System.Threading.Channels;
using DialogHostAvalonia;
using VRChatContentPublisher.App.ViewModels.Dialogs;

namespace VRChatContentPublisher.App.Services.Dialog;

public sealed class DialogService
{
    public string DialogHostId { get; } = "MainWindow-" + Guid.NewGuid().ToString("D");

    public Task DialogHostReadTask => _dialogHostReadyTcs.Task;

    private readonly TaskCompletionSource _dialogHostReadyTcs = new();
    public void DialogHostReady() => _dialogHostReadyTcs.TrySetResult();

    public Channel<DialogChannelItem> DialogChannel { get; } = Channel.CreateUnbounded<DialogChannelItem>(
        new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask<object?> ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModelBase
    {
        var tcs = new TaskCompletionSource<object?>();

        dialogViewModel.CloseRequested += (_, e) => DialogHost.Close(DialogHostId, e);
        await DialogChannel.Writer.WriteAsync(new DialogChannelItem(dialogViewModel, tcs));

        return await tcs.Task;
    }
}

public sealed record DialogChannelItem(DialogViewModelBase Dialog, TaskCompletionSource<object?> TaskCompletionSource);