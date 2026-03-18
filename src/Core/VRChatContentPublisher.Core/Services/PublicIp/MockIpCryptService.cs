using System.Security.Cryptography;
using System.Text;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.PublicIp;

public sealed class MockIpCryptService(IWritableOptions<IpCryptStorage> storage) : IIpCryptService
{
    private const string Prefix = "mock-ipcrypt-pfx:";

    public async ValueTask<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var key = await GetOrCreateKeyAsync();
        var data = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = XorBytes(data, key);

        return Prefix + Convert.ToBase64String(encrypted);
    }

    public async ValueTask<string> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedText);

        if (!encryptedText.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid encrypted IP format.");

        var payload = encryptedText[Prefix.Length..];
        var encryptedBytes = Convert.FromBase64String(payload);

        var key = await GetOrCreateKeyAsync();
        var decrypted = XorBytes(encryptedBytes, key);

        return Encoding.UTF8.GetString(decrypted);
    }

    private async ValueTask<byte[]> GetOrCreateKeyAsync()
    {
        if (storage.Value.Key is { } keyText && !string.IsNullOrWhiteSpace(keyText))
        {
            return Convert.FromBase64String(keyText);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        await storage.UpdateAsync(s => s.Key = Convert.ToBase64String(key));
        return key;
    }

    private static byte[] XorBytes(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key)
    {
        var output = new byte[input.Length];

        for (var i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ key[i % key.Length]);
        }

        return output;
    }
}

