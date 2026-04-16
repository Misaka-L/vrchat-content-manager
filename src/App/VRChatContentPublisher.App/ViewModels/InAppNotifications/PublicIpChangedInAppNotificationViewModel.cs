using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MessagePipe;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Services.PublicIp;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed partial class PublicIpChangedInAppNotificationViewModel(
    PublicIpCheckerService publicIpCheckerService,
    ISubscriber<PublicIpChangedEvent> publicIpChangedSubscriber
) : InAppNotificationViewModelBase
{
    public string? LastPublicIp => publicIpCheckerService.LastPublicIp;
    public string? LastPreviousPublicIp => publicIpCheckerService.LastPreviousIp;

    private IDisposable? _eventSubscription;

    [RelayCommand]
    private void Load()
    {
        _eventSubscription = publicIpChangedSubscriber.Subscribe(_ =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                OnPropertyChanged(nameof(LastPublicIp));
                OnPropertyChanged(nameof(LastPreviousPublicIp));
            });
        });
    }

    [RelayCommand]
    private void Unload()
    {
        _eventSubscription?.Dispose();
        _eventSubscription = null;
    }

    [RelayCommand]
    private async Task Dismiss()
    {
        await publicIpCheckerService.DismissWarningAsync();
        RequestClose();
    }
}

public sealed class PublicIpChangedInAppNotificationViewModelFactory(
    PublicIpCheckerService publicIpCheckerService,
    ISubscriber<PublicIpChangedEvent> publicIpChangedSubscriber
)
{
    public PublicIpChangedInAppNotificationViewModel Create() => new(publicIpCheckerService, publicIpChangedSubscriber);
}