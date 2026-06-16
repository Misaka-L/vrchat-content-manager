using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace VRChatContentPublisher.App.Interop.Windows;

[SupportedOSPlatform("windows")]
public static partial class User32
{
    public const uint SPI_SETWORKAREA = 0x002F;
    public const uint SPI_GETWORKAREA = 0x0030;

    // winuser.h
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
    [LibraryImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool SystemParametersInfoA(uint uiAction, uint uiParam, void* pvParam, uint fWinIni);
}