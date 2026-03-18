using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Services.PublicIp;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class NotificationSettingsViewModel(
    IWritableOptions<AppSettings> appSettings,
    IIpCryptService ipCryptService) : ViewModelBase
{
    [ObservableProperty]
    public partial string EncryptedIpText { get; set; } = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasDecryptedIpText))]
    [ObservableProperty]
    public partial string DecryptedIpText { get; private set; } = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasDecryptErrorText))]
    [ObservableProperty]
    public partial string DecryptErrorText { get; private set; } = string.Empty;

    public bool HasDecryptedIpText => !string.IsNullOrWhiteSpace(DecryptedIpText);
    public bool HasDecryptErrorText => !string.IsNullOrWhiteSpace(DecryptErrorText);

    public bool SendNotificationOnStartupSessionRestoreFailed
    {
        get => appSettings.Value.SendNotificationOnStartupSessionRestoreFailed;
        set
        {
            if (appSettings.Value.SendNotificationOnStartupSessionRestoreFailed == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.SendNotificationOnStartupSessionRestoreFailed = value);
            OnPropertyChanged();
        }
    }

    public bool SendNotificationOnTaskFailed
    {
        get => appSettings.Value.SendNotificationOnTaskFailed;
        set
        {
            if (appSettings.Value.SendNotificationOnTaskFailed == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.SendNotificationOnTaskFailed = value);
            OnPropertyChanged();
        }
    }

    public bool SendNotificationOnPublicIpChanged
    {
        get => appSettings.Value.SendNotificationOnPublicIpChanged;
        set
        {
            if (appSettings.Value.SendNotificationOnPublicIpChanged == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.SendNotificationOnPublicIpChanged = value);
            OnPropertyChanged();
        }
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
}

