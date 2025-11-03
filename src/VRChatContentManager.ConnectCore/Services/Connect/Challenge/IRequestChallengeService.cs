namespace VRChatContentManager.ConnectCore.Services.Connect.Challenge;

public interface IRequestChallengeService
{
    Task RequestChallengeAsync(string code, string clientId, string identityPrompt);
    Task CompleteChallengeAsync(string clientId);
}