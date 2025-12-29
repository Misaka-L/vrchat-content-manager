using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;

namespace VRChatContentPublisher.App.Interaction;

public class LaunchDirectoryAction : StyledElementAction
{
    public static readonly StyledProperty<string?> DirectoryPathProperty =
        AvaloniaProperty.Register<LaunchUriAction, string?>(nameof(DirectoryPath));
    
    public string? DirectoryPath
    {
        get => GetValue(DirectoryPathProperty);
        set => SetValue(DirectoryPathProperty, value);
    }

    public override object Execute(object? sender, object? parameter)
    {
        if (!IsEnabled)
        {
            return false;
        }

        var directoryPath = DirectoryPath;
        if (string.IsNullOrEmpty(directoryPath))
        {
            return false;
        }

        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        var directoryInfo = new DirectoryInfo(directoryPath);

        var topLevel = TopLevel.GetTopLevel(sender as Visual);
        if (topLevel?.Launcher is { } launcher)
        {
            // Fire and forget
            _ = launcher.LaunchDirectoryInfoAsync(directoryInfo);
            return true;
        }

        return false;
    }
}