namespace VRChatContentManager.ConnectCore.Services.Health;

public interface IHealthService
{
    ValueTask<bool> IsReadyForPublishAsync();
}