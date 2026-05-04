namespace VRChatContentPublisher.Platform.Abstraction.Services;

public interface IDesktopNotificationService
{
    bool IsSupported { get; }

    ValueTask SendDesktopNotificationAsync(string title, string? message = null);
    ValueTask InitializeAsync();
}