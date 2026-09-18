using System.Runtime.Versioning;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using VRChatContentPublisher.Platform.Abstraction.Services;
using VRChatContentPublisher.Platform.Windows.Interop;

namespace VRChatContentPublisher.Platform.Windows.Services;

[SupportedOSPlatform("windows")]
public class WindowsDesktopNotificationService : IDesktopNotificationService
{
    /// <summary>
    /// GUID used as a stub toast activator CLSID. Desktop notification click is handled by protocol
    /// activation instead of a COM activator, but the shell still expects a toast activator CLSID to
    /// be bound to the app. No COM server is registered for this CLSID on purpose: that is the
    /// documented "no COM / stub CLSID" option, and protocol activation is the only activation type
    /// it supports.
    /// See https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-desktop-apps
    /// </summary>
    private const string NotificationActivatorClsid = "{2F1C0B7E-4D6A-4E36-9E10-5B7E8F0C2A44}";

    /// <summary>
    /// Command line argument used by the toast activator convention of <c>Microsoft.Toolkit.Uwp.Notifications</c>
    /// and <c>CommunityToolkit.WinUI.Notifications</c>. This app registers no <c>LocalServer32</c>, so
    /// the shell never appends it; it is recognized anyway so a leftover registration from a former
    /// version still activates the running instance instead of starting a second one.
    /// </summary>
    public const string ToastActivatedLaunchArgumentPrefix = "--toast-activated";

    private string? _notificationActivationUri;
    private ToastNotifier? _toastNotifier;

    public bool IsSupported => true;

    public ValueTask SendDesktopNotificationAsync(string title, string? message = null,
        string? actionButtonText = null)
    {
        if (_toastNotifier is null) return ValueTask.CompletedTask;

        var builder = new ToastContentBuilder();

        builder.AddText(title);

        if (!string.IsNullOrEmpty(message))
            builder.AddText(message);

        if (_notificationActivationUri is not null)
        {
            var activationUri = new Uri(_notificationActivationUri);

            // Bring the main window of the running instance to the foreground when the user clicks
            // the notification. Protocol activation is used because a COM activator requires
            // runtime code generation, which is not available on native AOT.
            builder.SetProtocolActivation(activationUri);

            // Fallback for the shells that do not activate the toast body through its protocol.
            if (!string.IsNullOrEmpty(actionButtonText))
                builder.AddButton(new ToastButton()
                    .SetContent(actionButtonText)
                    .SetProtocolActivation(activationUri));
        }

        _toastNotifier.Show(new ToastNotification(builder.GetXml()));

        return ValueTask.CompletedTask;
    }

    public ValueTask InitializeAsync(string notificationActivationUri)
    {
        _notificationActivationUri = notificationActivationUri;

        using var appIdSubKey =
            Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\" + WindowsConst.AppUserModelId);
        appIdSubKey.SetValue("DisplayName", "VRChat Content Publisher", RegistryValueKind.String);
        appIdSubKey.SetValue("Has7.0.1Fix", 1, RegistryValueKind.DWord);
        appIdSubKey.SetValue("IconUri", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotificationIcon.png"));
        appIdSubKey.SetValue("CustomActivator", NotificationActivatorClsid, RegistryValueKind.String);

        Shell32Interop.SetCurrentProcessExplicitAppUserModelID(WindowsConst.AppUserModelId);

        _toastNotifier = ToastNotificationManager.CreateToastNotifier(WindowsConst.AppUserModelId);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Extracts the activation URI from the process command line. The URI is passed as the first
    /// argument by the shell (protocol activation), or after <c>--url</c> because that is how the
    /// protocol handler is registered by the installer.
    /// </summary>
    public static string? TryGetNotificationActivationUri(
        IReadOnlyList<string> commandLineArgs,
        string notificationActivationUri,
        string toastActivatedLaunchArgumentPrefix)
    {
        for (var i = 0; i < commandLineArgs.Count; i++)
        {
            var argument = commandLineArgs[i];

            if (argument.StartsWith(toastActivatedLaunchArgumentPrefix, StringComparison.OrdinalIgnoreCase))
                return notificationActivationUri;

            if (i > 0 && commandLineArgs[i - 1].Equals("--url", StringComparison.OrdinalIgnoreCase))
                return argument;

            if (argument.StartsWith(notificationActivationUri, StringComparison.OrdinalIgnoreCase))
                return argument;
        }

        return null;
    }
}
