using System.Runtime.InteropServices;

namespace VRChatContentPublisher.Platform.Windows.Interop;

internal static partial class Shell32Interop
{
    [LibraryImport("shell32.dll", SetLastError = true)]
    internal static partial void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);
}