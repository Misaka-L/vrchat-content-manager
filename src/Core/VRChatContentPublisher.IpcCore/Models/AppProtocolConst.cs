namespace VRChatContentPublisher.IpcCore.Models;

public static class AppProtocolConst
{
    /// <summary>
    /// URI scheme for custom protocol activation declared by the installer, see
    /// <c>distribution/windows-installer-inno/installer.iss</c>.
    /// </summary>
    public const string Scheme = "vrchat-content-manager";

    /// <summary>
    /// Activation URI used by desktop notification click to bring the main window of the
    /// running instance to the foreground.
    /// </summary>
    public const string NotificationActivateUri = Scheme + "://notification-clicked";
}
