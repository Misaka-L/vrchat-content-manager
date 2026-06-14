using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.Core.Shared;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class DebugSettingsViewModel(
    NetworkDiagnostic.NetworkDiagnosticWindowViewModel networkDiagnosticWindowViewModel,
    NavigationService navigationService
) : ViewModelBase
{
    public string LogsPath => AppStorageService.GetLogsPath();

    [RelayCommand]
    private void RestartOnBoarding()
    {
        navigationService.Navigate<GuideWelcomePageViewModel>();
    }

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