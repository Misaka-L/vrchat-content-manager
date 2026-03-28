using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class StartupPortChangedDialogViewModel(
    int configuredPort,
    int activePort) : DialogViewModelBase
{
    public int ConfiguredPort { get; } = configuredPort;
    public int ActivePort { get; } = activePort;

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

