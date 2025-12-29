using VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services;

public sealed class ConnectMetadataProvider(IWritableOptions<AppSettings> appSettings) : IConnectMetadataProvider
{
    public string GetInstanceName() => appSettings.Value.ConnectInstanceName;

    public string GetImplementation() => "VRChatContentPublisher.Core";
    public string GetImplementationVersion() => "snapshot";
}