namespace VRChatContentManager.App.ViewModels.Dialogs;

public sealed class RequestChallengeDialogViewModel(string code, string clientId) : DialogViewModelBase
{
    public string Code { get; } = code;
    public string ClientId { get; } = clientId;
}

public sealed class RequestChallengeDialogViewModelFactory
{
    public RequestChallengeDialogViewModel Create(string code, string clientId)
    {
        return new RequestChallengeDialogViewModel(code, clientId);
    }
}