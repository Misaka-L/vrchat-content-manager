namespace VRChatContentPublisher.Core.Services.PublicIp;

public interface IIpCryptService
{
    ValueTask<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default);
    ValueTask<string> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default);
}