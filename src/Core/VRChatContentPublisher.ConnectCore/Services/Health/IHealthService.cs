namespace VRChatContentPublisher.ConnectCore.Services.Health;

public interface IHealthService
{
    ValueTask<bool> IsReadyForPublishAsync();
}