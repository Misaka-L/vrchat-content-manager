using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class AppearanceSettingsViewModel(
    IWritableOptions<AppSettings> appSettings,
    AppWindowService appWindowService
    ) : PageViewModelBase
{
    public bool UseRgbCyclingBackgroundMenu
    {
        get => appSettings.Value.UseRgbCyclingBackgroundMenu;
        set
        {
            if (appSettings.Value.UseRgbCyclingBackgroundMenu == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.UseRgbCyclingBackgroundMenu = value);
            OnPropertyChanged();
        }
    }

    public bool UseBorderlessWindow
    {
        get => appWindowService.IsBorderless();
        set
        {
            if (appWindowService.IsBorderless() == value)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.UseBorderlessWindow = value);
            _ = appWindowService.SetBorderlessAsync(value);
            OnPropertyChanged();
        }
    }

    public AppTasksPageSortModeItemViewModel SelectedTasksSortMode
    {
        get => TasksSortMode.First(x => x.Mode == appSettings.Value.TasksPageSortMode);
        set
        {
            if (SelectedTasksSortMode.Mode == value.Mode)
                return;

            OnPropertyChanging();
            appSettings.Update(settings => settings.TasksPageSortMode = value.Mode);
            OnPropertyChanged();
        }
    }

    public AppTasksPageSortModeItemViewModel[] TasksSortMode { get; } =
    [
        new("Latest first", AppTasksPageSortMode.LatestFirst),
        new("Oldest first", AppTasksPageSortMode.OldestFirst)
    ];
}

public record AppTasksPageSortModeItemViewModel(string Name, AppTasksPageSortMode Mode);