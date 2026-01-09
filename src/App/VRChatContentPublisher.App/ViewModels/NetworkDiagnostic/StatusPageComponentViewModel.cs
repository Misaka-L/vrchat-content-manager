using Avalonia.Collections;

namespace VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;

public sealed class StatusPageComponentViewModel(string id, string name, string status)
{
    public string Id => id;
    public string Name => name;
    public string Status => status;

    public AvaloniaList<StatusPageComponentViewModel> SubComponents { get; } = [];
}