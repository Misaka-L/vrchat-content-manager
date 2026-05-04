using Antelcat.I18N.Avalonia;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.App.Services.Notification;

public sealed class DesktopNotificationService(
    ILogger<DesktopNotificationService> logger,
    IDesktopNotificationService desktopNotificationService,
    IWritableOptions<AppSettings> appSettings)
{
    public bool IsSupported => desktopNotificationService.IsSupported;

    public bool IsInitialized { get; private set; }
    public Exception? LastInitializationException { get; private set; }

    public async ValueTask TryInitializeAsync()
    {
        if (!IsSupported || IsInitialized)
            return;

        try
        {
            await desktopNotificationService.InitializeAsync();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the desktop notification service");
            LastInitializationException = ex;
        }
    }

    public async ValueTask SendNotificationAsync(string title, string? message = null)
    {
        if (!appSettings.Value.NotificationsEnabled)
            return;

        if (!IsInitialized)
            return;

        var localizedTitle = I18NExtension.Translate(title) ?? title;
        var localizedMessage = message is not null ? I18NExtension.Translate(message, message) : message;

        await desktopNotificationService.SendDesktopNotificationAsync(localizedTitle, localizedMessage);
    }
}