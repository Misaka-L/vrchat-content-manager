namespace VRChatContentManager.ConnectCore.Services;

public sealed class DefaultRequestChallengeService : IRequestChallengeService
{
    public Task RequestChallengeAsync(string code, string clientId, string identityPrompt)
    {
        return Task.CompletedTask;
    }
}