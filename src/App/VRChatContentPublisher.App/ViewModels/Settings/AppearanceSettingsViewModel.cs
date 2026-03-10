using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class AppearanceSettingsViewModel(IWritableOptions<AppSettings> appSettings) : PageViewModelBase
{
    
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
}