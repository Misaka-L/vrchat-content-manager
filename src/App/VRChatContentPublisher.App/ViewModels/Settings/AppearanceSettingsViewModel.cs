using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class AppearanceSettingsViewModel(IWritableOptions<AppSettings> appSettings) : PageViewModelBase
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

    [ObservableProperty] public partial AppTasksPageSortModeItemViewModel SelectedTasksSortMode { get; set; }

    public AppTasksPageSortModeItemViewModel[] TasksSortMode { get; } =
    [
        new("Latest first", AppTasksPageSortMode.LatestFirst),
        new("Oldest first", AppTasksPageSortMode.OldestFirst)
    ];

    [RelayCommand]
    private void Load()
    {
        UpdateSettingsFromOptions();
    }

    private void UpdateSettingsFromOptions()
    {
        SelectedTasksSortMode = TasksSortMode.First(x => x.Mode == appSettings.Value.TasksPageSortMode);
    }

    [RelayCommand]
    private async Task ApplySettings()
    {
        await appSettings.UpdateAsync(settings => settings.TasksPageSortMode = SelectedTasksSortMode.Mode);

        UpdateSettingsFromOptions();
    }
}

public record AppTasksPageSortModeItemViewModel(string Name, AppTasksPageSortMode Mode);