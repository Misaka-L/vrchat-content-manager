namespace VRChatContentPublisher.ConnectCore.Services.Connect.SessionStorage;

public interface ITokenSecretKeyProvider
{
    ValueTask<byte[]> GetSecretKeyAsync();
}