using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Services.PublicIp;

namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed partial class PublicIpChangedInAppNotificationViewModel(
    PublicIpCheckerService publicIpCheckerService
) : InAppNotificationViewModelBase
{
    public string? LastPublicIp => publicIpCheckerService.LastPublicIp;
    public string? LastPreviousPublicIp => publicIpCheckerService.LastPreviousIp;

    [RelayCommand]
    private void Load()
    {
        publicIpCheckerService.PublicIpChanged += OnPublicIpChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        publicIpCheckerService.PublicIpChanged -= OnPublicIpChanged;
    }

    private void OnPublicIpChanged(object? sender, PublicIpChangedEvent e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            OnPropertyChanged(nameof(LastPublicIp));
            OnPropertyChanged(nameof(LastPreviousPublicIp));
        });
    }

    [RelayCommand]
    private async Task Dismiss()
    {
        await publicIpCheckerService.DismissWarningAsync();
        RequestClose();
    }
}

public sealed class PublicIpChangedInAppNotificationViewModelFactory(
    PublicIpCheckerService publicIpCheckerService
)
{
    public PublicIpChangedInAppNotificationViewModel Create() => new(publicIpCheckerService);
}