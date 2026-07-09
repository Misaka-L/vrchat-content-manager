using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using VRChatContentPublisher.App.Interop.Windows;
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
            HideWindow();
        };

        Deactivated += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel && (viewModel.Pinned || !viewModel.Borderless))
                return;
            HideWindow();
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

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        UpdateWindowConfiguration();
    }

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
        if (!OperatingSystem.IsWindows())
            return;
        if (Screens.Primary is not { } primaryScreen)
            return;

        var workingArea = primaryScreen.WorkingArea;
        var taskBarLocation = TaskBarHelper.GetTaskBarLocation(primaryScreen, workingArea);
        var taskBarHeight = TaskBarHelper.GetTaskBarHeight(primaryScreen, workingArea);

        var screenBoundsWithDpi = new PixelPoint(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
        var windowBoundsWithDpi =
            PixelPoint.FromPoint(new Point((int)Bounds.Size.Width, (int)Bounds.Size.Height), primaryScreen.Scaling);
        switch (taskBarLocation)
        {
            case TaskBarHelper.TaskBarLocation.Top:
                Position = screenBoundsWithDpi.WithY(0) - windowBoundsWithDpi.WithY(0) +
                           PixelPoint.FromPoint(new Point(0, taskBarHeight), primaryScreen.Scaling) -
                           PixelPoint.FromPoint(new Point(16, -12), primaryScreen.Scaling);
                break;
            case TaskBarHelper.TaskBarLocation.Bottom:
                Position = screenBoundsWithDpi - windowBoundsWithDpi -
                           PixelPoint.FromPoint(new Point(0, taskBarHeight), primaryScreen.Scaling) -
                           PixelPoint.FromPoint(new Point(16, 12), primaryScreen.Scaling);
                break;
            case TaskBarHelper.TaskBarLocation.Left:
                Position = screenBoundsWithDpi.WithX(0) - windowBoundsWithDpi.WithX(0) +
                           PixelPoint.FromPoint(new Point(taskBarHeight, 0), primaryScreen.Scaling) +
                           PixelPoint.FromPoint(new Point(0, -12), primaryScreen.Scaling);
                break;
            case TaskBarHelper.TaskBarLocation.Right:
                Position = screenBoundsWithDpi - windowBoundsWithDpi -
                           PixelPoint.FromPoint(new Point(taskBarHeight, 0), primaryScreen.Scaling) -
                           PixelPoint.FromPoint(new Point(16, 12), primaryScreen.Scaling);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
        Screens.Changed += OnScreensChanged;
    }

    private void Window_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        viewModel.RequestActivate -= RequestActive;
        viewModel.PropertyChanged -= OnPropertyChanged;
        Screens.Changed -= OnScreensChanged;
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
        if (WindowState == WindowState.Normal) UpdateWindowConfiguration();
    }

    private void HideWindow()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        viewModel.NotifyWindowHide();
        Hide();
    }
}