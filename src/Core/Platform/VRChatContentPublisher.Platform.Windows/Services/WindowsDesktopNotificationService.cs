using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using VRChatContentPublisher.Platform.Abstraction.Services;
using VRChatContentPublisher.Platform.Windows.Interop;

namespace VRChatContentPublisher.Platform.Windows.Services;

public class WindowsDesktopNotificationService : IDesktopNotificationService
{
    private readonly ToastNotifier _toastNotifier =
        ToastNotificationManager.CreateToastNotifier(WindowsConst.AppUserModelId);

    public ValueTask SendDesktopNotificationAsync(string title, string? message = null)
    {
        var builder = new ToastContentBuilder();

        builder.AddText(title);

        if (!string.IsNullOrEmpty(message))
            builder.AddText(message);

        _toastNotifier.Show(new ToastNotification(builder.GetXml()));

        return ValueTask.CompletedTask;
    }

    internal void Initialize()
    {
        using var appIdSubKey =
            Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\" + WindowsConst.AppUserModelId);
        appIdSubKey.SetValue("DisplayName", "VRChat Content Publisher", RegistryValueKind.String);
        appIdSubKey.SetValue("Has7.0.1Fix", 1, RegistryValueKind.DWord);
        appIdSubKey.SetValue("IconUri", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotificationIcon.png"));

        Shell32Interop.SetCurrentProcessExplicitAppUserModelID(WindowsConst.AppUserModelId);
    }
}