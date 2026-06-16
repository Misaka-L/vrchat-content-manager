using System.Runtime.Versioning;
using Avalonia.Controls;

namespace VRChatContentPublisher.App.Interop.Windows;

[SupportedOSPlatform("windows")]
public static class WorkingAreaHelper
{
    public static Win32Rect GetWorkingArea()
    {
        unsafe
        {
            var rect = new Win32Rect();
            User32.SystemParametersInfoA(User32.SPI_GETWORKAREA, 0, &rect, 0);

            return rect;
        }
    }

    extension(Win32Properties)
    {
        public static void AddWorkingAreaChangedListener(TopLevel topLevel, Action callback)
        {Win32Properties.AddWndProcHookCallback(topLevel, (_, msg, wParam, lParam, ref handled) =>
            {
                // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-settingchange
                // WM_SETTINGCHANGE
                if (msg != 0x001A)
                    return 0;

                //https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
                // SPI_GETWORKAREA
                if (wParam != User32.SPI_SETWORKAREA)
                    return 0;

                callback();

                return 0;
            });
        }
    }
}