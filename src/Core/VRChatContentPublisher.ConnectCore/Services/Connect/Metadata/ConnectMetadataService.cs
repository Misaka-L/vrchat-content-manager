namespace VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;

public sealed class ConnectMetadataService(IConnectMetadataProvider metadataProvider)
{
    public string GetInstanceName() => metadataProvider.GetInstanceName();

    public string GetImplementation() => metadataProvider.GetImplementation();
    public string GetImplementationVersion() => metadataProvider.GetImplementationVersion();
    
    public string[] GetFeatureFlags() => metadataProvider.GetFeatureFlags();

    public string GetApiVersion() => "1.1.0";
}