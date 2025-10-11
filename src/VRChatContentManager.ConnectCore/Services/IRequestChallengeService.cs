namespace VRChatContentManager.ConnectCore.Services;

public interface IRequestChallengeService
{
    Task RequestChallengeAsync(string code, string clientId, string identityPrompt);
}