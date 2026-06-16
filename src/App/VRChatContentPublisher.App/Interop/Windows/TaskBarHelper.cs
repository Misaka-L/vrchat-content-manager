using Avalonia.Platform;

namespace VRChatContentPublisher.App.Interop.Windows;

public static class TaskBarHelper
{
    public enum TaskBarLocation
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public static TaskBarLocation GetTaskBarLocation(Screen screen, Win32Rect rect)
    {
        var taskBarOnTopOrBottom = rect.right - rect.left == screen.Bounds.Width;
        if (taskBarOnTopOrBottom)
        {
            if (rect.top > 0)
                return TaskBarLocation.Top;

            return TaskBarLocation.Bottom;
        }

        return rect.left > 0 ? TaskBarLocation.Left : TaskBarLocation.Right;
    }

    public static int GetTaskBarHeight(Screen screen, Win32Rect rect)
    {
        var taskbarHeight = GetTaskBarLocation(screen, rect) switch
        {
            TaskBarLocation.Top => rect.top,
            TaskBarLocation.Bottom => screen.Bounds.Height - rect.bottom,
            TaskBarLocation.Left => rect.left,
            TaskBarLocation.Right => screen.Bounds.Width - rect.right,
            _ => throw new ArgumentOutOfRangeException()
        };

        return (int)(taskbarHeight / screen.Scaling);
    }
}