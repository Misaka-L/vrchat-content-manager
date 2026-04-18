using Avalonia.Collections;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.ViewModels.InAppNotifications;

namespace VRChatContentPublisher.App.Services;

public sealed class InAppNotificationService(IServiceProvider serviceProvider)
{
    public IAvaloniaReadOnlyList<InAppNotificationViewModelBase> Notifications => _notifications;

    private readonly AvaloniaList<InAppNotificationViewModelBase> _notifications = [];

    public void SendNotification(InAppNotificationViewModelBase notification)
    {
        notification.CloseRequested += NotificationOnCloseRequested;
        _notifications.Add(notification);
    }

    public void SendNotification<TNotification>() where TNotification : InAppNotificationViewModelBase
    {
        var notification = serviceProvider.GetRequiredService<TNotification>();
        SendNotification(notification);
    }

    public void RemoveNotificationOfType<T>() where T : InAppNotificationViewModelBase
    {
        var notificationToRemove = _notifications
            .Where(x => x is T)
            .ToArray();

        foreach (var notification in notificationToRemove)
        {
            RemoveNotification(notification);
        }
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