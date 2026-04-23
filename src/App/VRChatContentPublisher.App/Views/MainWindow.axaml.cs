using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using VRChatContentPublisher.App.ViewModels;

namespace VRChatContentPublisher.App.Views;

public partial class MainWindow : Window
{
    private WindowState _lastStateBeforeMinimized;

    public MainWindow()
    {
        InitializeComponent();

        UpdateWindowConfiguration();

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
            if (DataContext is MainWindowViewModel viewModel && (viewModel.Pinned || !viewModel.Borderless))
                return;
#if !DEBUG
            Hide();
#endif
        };

        _lastStateBeforeMinimized = WindowState;
        this.GetObservable(WindowStateProperty)
            .Subscribe(new AnonymousObserver<WindowState>(state =>
            {
                if (state == WindowState.Minimized)
                    return;

                _lastStateBeforeMinimized = state;
            }));
    }

    private bool IsBorderlessSupported() => OperatingSystem.IsWindows();

    #region Window Configuration

    private void UpdateWindowConfiguration()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        if (!IsBorderlessSupported() || !viewModel.Borderless)
        {
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ExtendClientAreaToDecorationsHint = false;
            WindowDecorations = WindowDecorations.Full;
            Topmost = viewModel.Pinned;

            return;
        }

        ExtendClientAreaToDecorationsHint = true;
        WindowDecorations = WindowDecorations.BorderOnly;
        WindowState = WindowState.Normal;
#if DEBUG
        ShowInTaskbar = true;
#else
        ShowInTaskbar = false;
#endif
        Topmost = true;

        UpdateWindowPosition();
    }

    private void UpdateWindowPosition()
    {
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
                           PixelPoint.FromPoint(new Point(16, 12), primaryScreen.Scaling);
                break;
            case TaskBarLocation.Left:
                break;
            case TaskBarLocation.Right:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion

    #region TaskBar Helper

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

    #endregion

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        UpdateWindowConfiguration();
    }

    private void Window_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        viewModel.RequestActivate += RequestActive;
        viewModel.PropertyChanged += OnPropertyChanged;
    }

    private void Window_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        viewModel.RequestActivate -= RequestActive;
        viewModel.PropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.Borderless) or nameof(MainWindowViewModel.Pinned))
        {
            UpdateWindowConfiguration();
        }
    }

    private void RequestActive(object? sender, EventArgs e)
    {
        Show();
        Activate();

        WindowState = _lastStateBeforeMinimized;
    }
}