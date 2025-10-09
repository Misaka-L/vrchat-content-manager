namespace VRChatContentManager.ConnectCore.Models.ClientSession;

public record ChallengeSession(string Code, string ClientId, DateTimeOffset Expires);