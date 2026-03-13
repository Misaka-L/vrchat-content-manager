using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class StartupPortChangedDialogViewModel(
    int configuredPort,
    int activePort) : DialogViewModelBase
{
    public int ConfiguredPort { get; } = configuredPort;
    public int ActivePort { get; } = activePort;

    public string Description =>
        $"Configured RPC port {ConfiguredPort} is in use. RPC server started on fallback port {ActivePort}.";

    [RelayCommand]
    private void Acknowledge()
    {
        RequestClose(true);
    }
}

public sealed class StartupPortChangedDialogViewModelFactory
{
    public StartupPortChangedDialogViewModel Create(int configuredPort, int activePort)
    {
        return new StartupPortChangedDialogViewModel(configuredPort, activePort);
    }
}

