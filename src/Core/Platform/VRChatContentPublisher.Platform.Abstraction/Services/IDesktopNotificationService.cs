namespace VRChatContentPublisher.Platform.Abstraction.Services;

public interface IDesktopNotificationService
{
    ValueTask SendDesktopNotificationAsync(string title, string? message = null);
}