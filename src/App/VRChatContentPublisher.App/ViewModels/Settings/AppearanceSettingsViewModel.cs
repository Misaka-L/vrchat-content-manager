using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class AppearanceSettingsViewModel : PageViewModelBase
{
    public AppLang[] AvailableLanguages { get; }

    // ReSharper disable once ReplaceWithFieldKeyword
    private readonly AppLang _followSystemLang =
        new(
            LangKeys.Pages_Settings_Appearance_Language_Selector_Follow_System,
            LangKeys.Pages_Settings_Appearance_Language_Selector_Follow_System
        );

    private readonly IWritableOptions<AppSettings> _appSettings;
    private readonly AppWindowService _appWindowService;

    public AppearanceSettingsViewModel(
        IWritableOptions<AppSettings> appSettings,
        AppWindowService appWindowService
    )
    {
        _appSettings = appSettings;
        _appWindowService = appWindowService;

        AvailableLanguages =
        [
            _followSystemLang,
            ..AppLocalizationService.GetLanguages()
        ];
    }

    public AppLang SelectedLanguage
    {
        get
        {
            if (_appSettings.Value.AppCulture is null)
                return _followSystemLang;

            return AvailableLanguages.First(x => x.CultureCode == _appSettings.Value.AppCulture);
        }
        set
        {
            if (value == _followSystemLang)
            {
                if (_appSettings.Value.AppCulture is null)
                    return;

                OnPropertyChanging();
                UpdateAppCulture(null);
                OnPropertyChanged();
                return;
            }

            if (_appSettings.Value.AppCulture == value.CultureCode)
                return;

            OnPropertyChanging();
            UpdateAppCulture(value.CultureCode);
            OnPropertyChanged();
        }
    }

    public bool UseRgbCyclingBackgroundMenu
    {
        get => _appSettings.Value.UseRgbCyclingBackgroundMenu;
        set
        {
            if (_appSettings.Value.UseRgbCyclingBackgroundMenu == value)
                return;

            OnPropertyChanging();
            _appSettings.Update(settings => settings.UseRgbCyclingBackgroundMenu = value);
            OnPropertyChanged();
        }
    }

    public bool UseBorderlessWindow
    {
        get => _appWindowService.IsBorderless();
        set
        {
            if (_appWindowService.IsBorderless() == value)
                return;

            OnPropertyChanging();
            _appSettings.Update(settings => settings.UseBorderlessWindow = value);
            _ = _appWindowService.SetBorderlessAsync(value);
            OnPropertyChanged();
        }
    }

    public AppTasksPageSortModeItemViewModel SelectedTasksSortMode
    {
        get => TasksSortMode.First(x => x.Mode == _appSettings.Value.TasksPageSortMode);
        set
        {
            if (SelectedTasksSortMode.Mode == value.Mode)
                return;

            OnPropertyChanging();
            _appSettings.Update(settings => settings.TasksPageSortMode = value.Mode);
            OnPropertyChanged();
        }
    }

    public AppTasksPageSortModeItemViewModel[] TasksSortMode { get; } =
    [
        new(LangKeys.Pages_Settings_Appearance_How_Tasks_Order_In_Tasks_Page_Selector_Latest_First,
            AppTasksPageSortMode.LatestFirst),
        new(LangKeys.Pages_Settings_Appearance_How_Tasks_Order_In_Tasks_Page_Selector_Oldest_First,
            AppTasksPageSortMode.OldestFirst)
    ];

    private void UpdateAppCulture(string? cultureCode)
    {
        AppLocalizationService.ReloadAppCulture(cultureCode);
        _appSettings.Update(settings => settings.AppCulture = cultureCode);
    }
}

public record AppTasksPageSortModeItemViewModel(string Name, AppTasksPageSortMode Mode);