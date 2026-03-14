using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class NotificationSettingsViewModel(IWritableOptions<AppSettings> appSettings) : ViewModelBase
{
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
}

