namespace VRChatContentPublisher.App.ViewModels.InAppNotifications;

public sealed class SimpleInAppNotificationViewModel(
    SimpleInAppNotificationType type,
    string title,
    string message
) : InAppNotificationViewModelBase
{
    public string BackgroundColor => type switch
    {
        SimpleInAppNotificationType.Info => "#3F51B5",
        SimpleInAppNotificationType.Warning => "#FF6D00",
        SimpleInAppNotificationType.Error => "#D50000",
        _ => "#3F51B5"
    };

    public string Title => title;
    public string Message => message;
}

public enum SimpleInAppNotificationType
{
    Info,
    Warning,
    Error
}