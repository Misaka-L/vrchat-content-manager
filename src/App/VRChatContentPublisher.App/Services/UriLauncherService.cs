using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;

namespace VRChatContentPublisher.App.Services;

/// <summary>
/// Launches URIs with the shell default handler, and reports whether the URI was actually handled.
/// </summary>
public sealed class UriLauncherService(ILogger<UriLauncherService> logger)
{
    public async Task<bool> LaunchUriAsync(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
            return false;

        return await LaunchUriAsync(parsedUri);
    }

    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        var mainWindow =
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (TopLevel.GetTopLevel(mainWindow)?.Launcher is not { } launcher)
            return false;

        try
        {
            return await launcher.LaunchUriAsync(uri);
        }
        catch (Exception exception)
        {
            // Some platform launcher implementations (or a broken shell handler) throw
            // instead of returning false, so normalize them to a failed launch.
            logger.LogWarning(exception, "Failed to launch uri {Uri}", uri);
            return false;
        }
    }
}
