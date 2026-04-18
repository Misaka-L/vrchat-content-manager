using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.Platform.Noop.Services;

public sealed class NoopUpdateInstallationService : IUpdateInstallationService
{
    public bool IsUpdateInstallationSupported() => false;
    public string GetPlatformIdentifier() => "noop";

    public ValueTask InstallUpdateAsync(string pathToBundle)
    {
        throw new NotSupportedException();
    }
}