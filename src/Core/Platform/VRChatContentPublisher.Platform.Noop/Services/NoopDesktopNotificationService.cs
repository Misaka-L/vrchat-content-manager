using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.Platform.Noop.Services;

public sealed class NoopDesktopNotificationService : IDesktopNotificationService
{
    public ValueTask SendDesktopNotificationAsync(string title, string? message = null)
    {
        return ValueTask.CompletedTask;
    }
}