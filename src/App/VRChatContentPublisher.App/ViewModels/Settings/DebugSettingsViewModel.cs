using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class DebugSettingsViewModel(
    NetworkDiagnostic.NetworkDiagnosticWindowViewModel networkDiagnosticWindowViewModel
) : ViewModelBase
{
    public string LogsPath => AppStorageService.GetLogsPath();

    [RelayCommand]
    private void OpenNetworkDiagnosticWindow()
    {
        var window = new NetworkDiagnosticWindow
        {
            DataContext = networkDiagnosticWindowViewModel
        };

        window.Show();
    }
}