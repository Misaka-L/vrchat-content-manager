using Avalonia;
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

    public static TaskBarLocation GetTaskBarLocation(Screen screen, PixelRect rect)
    {
        var taskBarOnTopOrBottom = rect.Width == screen.Bounds.Width;
        if (taskBarOnTopOrBottom)
        {
            if (rect.Y > 0)
                return TaskBarLocation.Top;

            return TaskBarLocation.Bottom;
        }

        return rect.X > 0 ? TaskBarLocation.Left : TaskBarLocation.Right;
    }

    public static int GetTaskBarHeight(Screen screen, PixelRect rect)
    {
        var taskbarHeight = GetTaskBarLocation(screen, rect) switch
        {
            TaskBarLocation.Top => rect.Y,
            TaskBarLocation.Bottom => screen.Bounds.Height - rect.Height,
            TaskBarLocation.Left => rect.X,
            TaskBarLocation.Right => screen.Bounds.Width - rect.Width,
            _ => throw new ArgumentOutOfRangeException()
        };

        return (int)(taskbarHeight / screen.Scaling);
    }
}