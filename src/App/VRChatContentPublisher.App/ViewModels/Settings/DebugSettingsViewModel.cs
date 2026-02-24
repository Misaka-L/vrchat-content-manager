using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class DebugSettingsViewModel(
    NetworkDiagnostic.NetworkDiagnosticWindowViewModel networkDiagnosticWindowViewModel,
    IWritableOptions<AppSettings> appSettings
) : ViewModelBase
{
    public string LogsPath => AppStorageService.GetLogsPath();

    public bool UseRgbCyclingBackgroundMenu
    {
        get;
        set
        {
            if (field == value)
                return;

            OnPropertyChanging();
            appSettings.UpdateAsync(settings => { settings.UseRgbCyclingBackgroundMenu = value; });
        }
    } = appSettings.Value.UseRgbCyclingBackgroundMenu;

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