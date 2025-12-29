using System.Security.Cryptography;
using VRChatContentPublisher.ConnectCore.Services.Connect.SessionStorage;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services;

public sealed class RpcTokenSecretKeyProvider(IWritableOptions<RpcSessionStorage> storage) : ITokenSecretKeyProvider
{
    public async ValueTask<byte[]> GetSecretKeyAsync()
    {
        if (storage.Value.SecretKey is { } secretKey)
        {
            var secretKeyBytes = new byte[64];
            if (Convert.TryFromBase64String(secretKey, secretKeyBytes, out var bytesWritten) && bytesWritten == 64)
            {
                return secretKeyBytes;
            }
        }
        
        var newKey = GenerateKey();
        await storage.UpdateAsync(s => s.SecretKey = Convert.ToBase64String(newKey));
        return newKey;
    }

    private static byte[] GenerateKey()
    {
        using var sha256 = new HMACSHA256();
        return sha256.Key;
    }
}