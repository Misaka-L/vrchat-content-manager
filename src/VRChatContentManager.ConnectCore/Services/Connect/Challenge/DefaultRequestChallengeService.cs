namespace VRChatContentManager.ConnectCore.Services.Connect.Challenge;

public sealed class DefaultRequestChallengeService : IRequestChallengeService
{
    public Task RequestChallengeAsync(string code, string clientId, string identityPrompt)
    {
        return Task.CompletedTask;
    }
}