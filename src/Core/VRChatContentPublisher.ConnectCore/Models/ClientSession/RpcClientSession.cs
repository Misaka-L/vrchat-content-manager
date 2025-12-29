namespace VRChatContentPublisher.ConnectCore.Models.ClientSession;

public record RpcClientSession(
    string ClientId,
    DateTimeOffset Expires,
    string ClientName = "Unknown Client"
);