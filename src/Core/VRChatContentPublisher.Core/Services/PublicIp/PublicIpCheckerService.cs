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
    public bool IsWarningDismissed => ipStateStorage.Value.IsWarningDismissed;
    public DateTimeOffset? LastCheckedAtUtc => ipStateStorage.Value.LastCheckedAtUtc;
    public DateTimeOffset? LastChangedAtUtc => ipStateStorage.Value.LastChangedAtUtc;
    public string? LastPublicIp => ipStateStorage.Value.LastPublicIp;
    public string? LastPreviousIp => ipStateStorage.Value.LastPreviousIp;

    public async ValueTask DismissWarningAsync()
    {
        await ipStateStorage.UpdateAsync(storage => storage.IsWarningDismissed = true);
    }

    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public async ValueTask RequestCheckAndPublishIfChangedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _checkLock.WaitAsync(0, cancellationToken))
            return;

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

            var encryptedOldIp = await ipCryptService.EncryptAsync(previousIp, cancellationToken);
            var encryptedNewIp = await ipCryptService.EncryptAsync(currentIp, cancellationToken);

            await ipStateStorage.UpdateAsync(storage =>
            {
                storage.LastPreviousIp = previousIp;
                storage.LastPublicIp = currentIp;
                storage.LastCheckedAtUtc = now;
                storage.LastChangedAtUtc = now;
                storage.IsWarningDismissed = false;
            });

            logger.LogWarning(
                "Public public IP changed. OldIpEncrypted: {OldIpEncrypted}, NewIpEncrypted: {NewIpEncrypted}",
                encryptedOldIp,
                encryptedNewIp);

            ipChangedPublisher.Publish(new PublicIpChangedEvent(
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