using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.Platform.Noop.Services;

public sealed class NoopDesktopNotificationService : IDesktopNotificationService
{
    public bool IsSupported => false;

    public ValueTask SendDesktopNotificationAsync(string title, string? message = null)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }
}