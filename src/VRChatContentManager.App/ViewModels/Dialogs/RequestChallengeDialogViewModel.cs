namespace VRChatContentManager.App.ViewModels.Dialogs;

public sealed class RequestChallengeDialogViewModel(string code, string clientId, string identityPrompt) : DialogViewModelBase
{
    public string Code { get; } = code;
    public string ClientId { get; } = clientId;
    public string IdentityPrompt { get; } = identityPrompt;
}

public sealed class RequestChallengeDialogViewModelFactory
{
    public RequestChallengeDialogViewModel Create(string code, string clientId, string identityPrompt)
    {
        return new RequestChallengeDialogViewModel(code, clientId, identityPrompt);
    }
}