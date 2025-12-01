using System;
using Avalonia;
using Avalonia.Controls;
using VRChatContentManager.App.ViewModels;

namespace VRChatContentManager.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (!OperatingSystem.IsWindows())
        {
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaToDecorationsHint = false;
            SystemDecorations = SystemDecorations.Full;
            Topmost = false;

            return;
        }

#if DEBUG
        ShowInTaskbar = true;
#endif

        Activate();

        Closing += (_, arg) =>
        {
            arg.Cancel = true;
            Hide();
        };

        Deactivated += (_, _) =>
        {
            if (DataContext is MainWindowViewModel { Pinned: true })
                return;
#if !DEBUG
            Hide();
#endif
        };

        // TransparencyLevelHint = [WindowTransparencyLevel.Mica];
    }

    private enum TaskBarLocation
    {
        Top,
        Bottom,
        Left,
        Right,
        Unknown
    }

    private TaskBarLocation GetTaskBarLocation()
    {
        if (Screens.Primary is not { } primaryScreen)
            return TaskBarLocation.Unknown;

        var taskBarOnTopOrBottom = primaryScreen.WorkingArea.Width == primaryScreen.Bounds.Width;
        if (taskBarOnTopOrBottom)
        {
            if (primaryScreen.WorkingArea.TopLeft.Y > 0)
                return TaskBarLocation.Top;
        }
        else
        {
            return primaryScreen.WorkingArea.TopLeft.X > 0 ? TaskBarLocation.Left : TaskBarLocation.Right;
        }

        return TaskBarLocation.Bottom;
    }

    private int GetTaskBarHeight()
    {
        if (Screens.Primary is not { } primaryScreen)
            return -1;

        var taskbarHeight = GetTaskBarLocation() switch
        {
            TaskBarLocation.Top => primaryScreen.WorkingArea.Height,
            TaskBarLocation.Bottom => primaryScreen.Bounds.Height - primaryScreen.WorkingArea.Height,
            TaskBarLocation.Left => primaryScreen.Bounds.Width - primaryScreen.WorkingArea.Width,
            TaskBarLocation.Right => primaryScreen.Bounds.Height - primaryScreen.WorkingArea.Height,
            _ => throw new ArgumentOutOfRangeException()
        };

        return (int)(taskbarHeight / primaryScreen.Scaling);
    }

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        UpdateWindowPosition();
    }

    private void UpdateWindowPosition()
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (Screens.Primary is not { } primaryScreen)
            return;

        var taskBarLocation = GetTaskBarLocation();
        var taskBarHeight = GetTaskBarHeight();

        var screenBoundsWithDpi = new PixelPoint(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
        var windowBoundsWithDpi =
            PixelPoint.FromPoint(new Point((int)Bounds.Size.Width, (int)Bounds.Size.Height), primaryScreen.Scaling);
        switch (taskBarLocation)
        {
            case TaskBarLocation.Top:
                break;
            case TaskBarLocation.Bottom:
                Position = screenBoundsWithDpi - windowBoundsWithDpi -
                           PixelPoint.FromPoint(new Point(0, taskBarHeight), primaryScreen.Scaling) -
                           PixelPoint.FromPoint(new Point(4, 4), primaryScreen.Scaling);
                break;
            case TaskBarLocation.Left:
                break;
            case TaskBarLocation.Right:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}