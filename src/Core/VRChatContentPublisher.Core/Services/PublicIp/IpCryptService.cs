using System.Security.Cryptography;
using System.Text;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.PublicIp;

public sealed partial class IpCryptService(IWritableOptions<IpCryptStorage> storage) : IIpCryptService
{
    public async ValueTask<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        var aes = await GetOrCreateAesAsync();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        using var encryptor = aes.CreateEncryptor();
        return Convert.ToHexStringLower(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
    }

    public async ValueTask<string> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default)
    {
        var aes = await GetOrCreateAesAsync();
        var bytes = Convert.FromHexString(encryptedText);
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private Aes? _aes;

    private async ValueTask<Aes> GetOrCreateAesAsync()
    {
        if (_aes is not null)
            return _aes;

        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Key = await GetOrCreateKeyAsync();

        _aes = aes;
        return _aes;
    }

    private async ValueTask<byte[]> GetOrCreateKeyAsync()
    {
        if (storage.Value.Key is { } keyText && !string.IsNullOrWhiteSpace(keyText))
        {
            return Convert.FromBase64String(keyText);
        }

        var key = RandomNumberGenerator.GetBytes(16);
        await storage.UpdateAsync(s => s.Key = Convert.ToBase64String(key));
        return key;
    }
}