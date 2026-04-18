namespace VRChatContentPublisher.Platform.Abstraction.Services;

public interface IUpdateInstallationService
{
    bool IsUpdateInstallationSupported();
    string GetPlatformIdentifier();
    ValueTask InstallUpdateAsync(string pathToBundle);
}