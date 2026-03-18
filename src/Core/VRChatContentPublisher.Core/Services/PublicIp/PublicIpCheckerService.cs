using MessagePipe;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.PublicIp;

public sealed class PublicIpCheckerService(
    CloudflareTracePublicIpProvider publicIpProvider,
    IIpCryptService ipCryptService,
    IWritableOptions<PublicIpStateStorage> ipStateStorage,
    IPublisher<PublicIpChangedEvent> ipChangedPublisher,
    ILogger<PublicIpCheckerService> logger)
{
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public async ValueTask CheckAndPublishIfChangedAsync(CancellationToken cancellationToken = default)
    {
        await _checkLock.WaitAsync(cancellationToken);
        try
        {
            var currentIp = await publicIpProvider.GetCurrentPublicIpAsync(cancellationToken);
            var previousIp = ipStateStorage.Value.LastPublicIp;
            var now = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(previousIp))
            {
                await ipStateStorage.UpdateAsync(storage =>
                {
                    storage.LastPublicIp = currentIp;
                    storage.LastCheckedAtUtc = now;
                });

                logger.LogInformation("Initialized baseline public IP state.");
                return;
            }

            if (string.Equals(previousIp, currentIp, StringComparison.Ordinal))
            {
                await ipStateStorage.UpdateAsync(storage => storage.LastCheckedAtUtc = now);
                return;
            }

            var warningInstanceId = Guid.CreateVersion7();
            var encryptedOldIp = await ipCryptService.EncryptAsync(previousIp, cancellationToken);
            var encryptedNewIp = await ipCryptService.EncryptAsync(currentIp, cancellationToken);

            await ipStateStorage.UpdateAsync(storage =>
            {
                storage.LastPublicIp = currentIp;
                storage.LastCheckedAtUtc = now;
                storage.LastChangedAtUtc = now;
                storage.LastWarningInstanceId = warningInstanceId;
            });

            logger.LogWarning(
                "Public public IP changed. WarningId: {WarningId}, OldIpEncrypted: {OldIpEncrypted}, NewIpEncrypted: {NewIpEncrypted}",
                warningInstanceId,
                encryptedOldIp,
                encryptedNewIp);

            ipChangedPublisher.Publish(new PublicIpChangedEvent(
                warningInstanceId,
                previousIp,
                currentIp,
                encryptedOldIp,
                encryptedNewIp,
                now));
        }
        finally
        {
            _checkLock.Release();
        }
    }
}

