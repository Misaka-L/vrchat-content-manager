using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Services.PublicIp;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class DebugSettingsViewModel(
    NetworkDiagnostic.NetworkDiagnosticWindowViewModel networkDiagnosticWindowViewModel,
    IIpCryptService ipCryptService
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

    [RelayCommand]
    private async Task DecryptIpAsync()
    {
        if (string.IsNullOrWhiteSpace(EncryptedIpText))
        {
            DecryptErrorText = "Please enter an encrypted IP text.";
            DecryptedIpText = string.Empty;
            return;
        }

        try
        {
            DecryptedIpText = await ipCryptService.DecryptAsync(EncryptedIpText.Trim());
            DecryptErrorText = string.Empty;
        }
        catch (Exception ex)
        {
            DecryptedIpText = string.Empty;
            DecryptErrorText = ex.Message;
        }
    }

    [ObservableProperty] public partial string EncryptedIpText { get; set; } = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasDecryptedIpText))]
    [ObservableProperty]
    public partial string DecryptedIpText { get; private set; } = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasDecryptErrorText))]
    [ObservableProperty]
    public partial string DecryptErrorText { get; private set; } = string.Empty;

    public bool HasDecryptedIpText => !string.IsNullOrWhiteSpace(DecryptedIpText);
    public bool HasDecryptErrorText => !string.IsNullOrWhiteSpace(DecryptErrorText);
}