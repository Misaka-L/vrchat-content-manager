using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using VRChatContentPublisher.Platform.Abstraction.Services;
using VRChatContentPublisher.Platform.Windows.Interop;

namespace VRChatContentPublisher.Platform.Windows.Services;

public class WindowsDesktopNotificationService : IDesktopNotificationService
{
    private ToastNotifier? _toastNotifier;

    public bool IsSupported => true;

    public ValueTask SendDesktopNotificationAsync(string title, string? message = null)
    {
        if (_toastNotifier is null) return ValueTask.CompletedTask;

        var builder = new ToastContentBuilder();

        builder.AddText(title);

        if (!string.IsNullOrEmpty(message))
            builder.AddText(message);

        _toastNotifier.Show(new ToastNotification(builder.GetXml()));

        return ValueTask.CompletedTask;
    }

    public ValueTask InitializeAsync()
    {
        using var appIdSubKey =
            Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\" + WindowsConst.AppUserModelId);
        appIdSubKey.SetValue("DisplayName", "VRChat Content Publisher", RegistryValueKind.String);
        appIdSubKey.SetValue("Has7.0.1Fix", 1, RegistryValueKind.DWord);
        appIdSubKey.SetValue("IconUri", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotificationIcon.png"));

        Shell32Interop.SetCurrentProcessExplicitAppUserModelID(WindowsConst.AppUserModelId);

        _toastNotifier = ToastNotificationManager.CreateToastNotifier(WindowsConst.AppUserModelId);
        return ValueTask.CompletedTask;
    }
}