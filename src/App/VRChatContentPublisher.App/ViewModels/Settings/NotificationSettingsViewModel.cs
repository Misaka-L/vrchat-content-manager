using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class NotificationSettingsViewModel(IWritableOptions<AppSettings> appSettings) : ViewModelBase
{
    public bool EnabledNotifications
    {
        get => appSettings.Value.NotificationsEnabled;
        set
        {
            if (appSettings.Value.NotificationsEnabled == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.NotificationsEnabled = value);
            OnPropertyChanged();
        }
    }

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
}