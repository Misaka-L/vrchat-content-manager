using Avalonia.Collections;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;

namespace VRChatContentPublisher.App.Services;

public sealed class InAppNotificationService
{
    public IAvaloniaReadOnlyList<InAppNotificationViewModelBase> Notifications => _notifications;

    private readonly AvaloniaList<InAppNotificationViewModelBase> _notifications = [];

    public void SendNotification(InAppNotificationViewModelBase notification)
    {
        notification.CloseRequested += NotificationOnCloseRequested;
        _notifications.Add(notification);
    }

    public void RemoveNotification(InAppNotificationViewModelBase notification)
    {
        notification.CloseRequested -= NotificationOnCloseRequested;
        _notifications.Remove(notification);
    }

    private void NotificationOnCloseRequested(object? sender, EventArgs e)
    {
        if (sender is InAppNotificationViewModelBase notification)
        {
            RemoveNotification(notification);
        }
    }
}