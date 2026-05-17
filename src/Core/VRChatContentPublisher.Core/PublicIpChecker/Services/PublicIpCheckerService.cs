using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.PublicIpChecker.IPCrypt;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.PublicIpChecker.Services;

public sealed class PublicIpCheckerService(
    CloudflareTracePublicIpProvider publicIpProvider,
    IIpCryptService ipCryptService,
    IWritableOptions<PublicIpStateStorage> ipStateStorage,
    ILogger<PublicIpCheckerService> logger)
{
    public event EventHandler<PublicIpChangedEvent>? PublicIpChanged;

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

            if (IsExactlySameIp(previousIp, currentIp))
            {
                await ipStateStorage.UpdateAsync(storage => storage.LastCheckedAtUtc = now);
                return;
            }

            if (IsSameIPv6NetworkDifferentHost(previousIp, currentIp))
            {
                // IPv6 host identifier changed within the same /64 prefix.
                // This is normal privacy extension behavior — log but do not notify the user.
                logger.LogInformation(
                    "IPv6 host identifier rotated within the same /64 network prefix. " +
                    "This is expected privacy extension behavior and does not indicate a network change.");
                await ipStateStorage.UpdateAsync(storage =>
                {
                    storage.LastPreviousIp = previousIp;
                    storage.LastPublicIp = currentIp;
                    storage.LastCheckedAtUtc = now;
                    storage.LastChangedAtUtc = now;
                });
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

            PublicIpChanged?.Invoke(this, new PublicIpChangedEvent(
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

    /// <summary>
    /// Returns <c>true</c> if both strings represent the exact same IP address.
    /// If either string is not a valid IP, logs a warning and falls back to
    /// ordinal string comparison.
    /// </summary>
    private bool IsExactlySameIp(string? previous, string? current)
    {
        if (!IPAddress.TryParse(previous, out var prevAddr) ||
            !IPAddress.TryParse(current, out var currAddr))
        {
            if (!IPAddress.TryParse(previous, out _))
                logger.LogWarning("Previous public IP is not a valid IP address: {PreviousIp}", previous);
            if (!IPAddress.TryParse(current, out _))
                logger.LogWarning("Current public IP is not a valid IP address: {CurrentIp}", current);

            return string.Equals(previous, current, StringComparison.Ordinal);
        }

        return prevAddr.Equals(currAddr);
    }

    /// <summary>
    /// Returns <c>true</c> when both addresses are IPv6, share the same first
    /// 64 bits (network prefix), but differ in the host portion — indicating
    /// a normal privacy-extension rotation rather than a genuine network change.
    /// </summary>
    private static bool IsSameIPv6NetworkDifferentHost(string? previous, string? current)
    {
        if (!IPAddress.TryParse(previous, out var prevAddr) ||
            !IPAddress.TryParse(current, out var currAddr))
        {
            return false;
        }

        if (prevAddr.AddressFamily != AddressFamily.InterNetworkV6 ||
            currAddr.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var prevBytes = prevAddr.GetAddressBytes();
        var currBytes = currAddr.GetAddressBytes();

        // Compare the first 8 bytes (64-bit network prefix).
        for (var i = 0; i < 8; i++)
        {
            if (prevBytes[i] != currBytes[i])
                return false;
        }

        // Ensure they are not identical (otherwise IsExactlySameIp would have caught it).
        for (var i = 8; i < 16; i++)
        {
            if (prevBytes[i] != currBytes[i])
                return true;
        }

        return false;
    }
}