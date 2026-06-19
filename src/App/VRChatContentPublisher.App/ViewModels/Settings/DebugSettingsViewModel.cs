using CommunityToolkit.Mvvm.Input;
using MessagePipe;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Shared;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class DebugSettingsViewModel(
    NetworkDiagnostic.NetworkDiagnosticWindowViewModel networkDiagnosticWindowViewModel,
    NavigationService navigationService,
    IPublisher<RequestBackgroundPublicIpCheckRunEvent> requestPublicIpCheckRunPublisher
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

    [RelayCommand]
    private void RequestBackgroundPublicIpCheckRun()
    {
        requestPublicIpCheckRunPublisher.Publish(new RequestBackgroundPublicIpCheckRunEvent());
    }
}