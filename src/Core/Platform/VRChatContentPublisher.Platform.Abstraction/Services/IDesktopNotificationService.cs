namespace VRChatContentPublisher.Platform.Abstraction.Services;

public interface IDesktopNotificationService
{
    bool IsSupported { get; }

    /// <param name="actionButtonText">
    /// Text of the action button that activates <paramref name="notificationActivationUri"/>, or null
    /// to send the notification without an action button.
    /// </param>
    ValueTask SendDesktopNotificationAsync(string title, string? message = null, string? actionButtonText = null);

    /// <param name="notificationActivationUri">
    /// URI invoked by the platform when the user clicks a desktop notification. Used to bring the
    /// main window of the running instance to the foreground when the app is launched by that URI.
    /// </param>
    ValueTask InitializeAsync(string notificationActivationUri);
}
