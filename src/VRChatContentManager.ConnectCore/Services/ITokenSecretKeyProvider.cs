namespace VRChatContentManager.ConnectCore.Services;

public interface ITokenSecretKeyProvider
{
    ValueTask<byte[]> GetSecretKeyAsync();
}