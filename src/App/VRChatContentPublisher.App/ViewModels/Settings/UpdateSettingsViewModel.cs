using Antelcat.I18N.Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class UpdateSettingsViewModel(
    IWritableOptions<AppSettings> appSettings,
    AppUpdateCheckService appUpdateCheckService,
    AppUpdateService appUpdateService,
    UpdateAvailableDialogViewModelFactory availableDialogFactory,
    DialogService dialogService,
    UpdateDownloadProgressViewModel updateDownloadProgressViewModel
) : ViewModelBase
{
    [RelayCommand]
    private void Load()
    {
        appUpdateService.OnUpdateStateChanged += OnUpdateStateChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        appUpdateService.OnUpdateStateChanged -= OnUpdateStateChanged;
    }

    #region Update Progress

    public UpdateDownloadProgressViewModel UpdateDownloadProgressViewModel => updateDownloadProgressViewModel;
    public bool IsIdle => appUpdateService.UpdateState == AppUpdateServiceState.Idle;
    public bool IsUpdateInstallationSupported => appUpdateService.IsAppUpdateSupported();

    private void OnUpdateStateChanged(object? sender, AppUpdateServiceState e)
    {
        OnPropertyChanged(nameof(IsIdle));
    }

    #endregion

    #region Update Settings

    public UpdateSettingsUpdateCheckModeViewModel UpdateCheckMode
    {
        get => UpdateCheckModes.First(x => x.Mode == appSettings.Value.UpdateCheckMode);
        set
        {
            OnPropertyChanged();
            appSettings.Update(s => s.UpdateCheckMode = value.Mode);
            OnPropertyChanged();
        }
    }

    public UpdateSettingsUpdateCheckModeViewModel[] UpdateCheckModes { get; } =
    [
        new(LangKeys.Pages_Settings_Software_Update_Auto_Check_Update_Mode_Selector_Manual,
            AppUpdateCheckMode.Manual
        ),
        new(LangKeys.Pages_Settings_Software_Update_Auto_Check_Update_Mode_Selector_Only_At_Start,
            AppUpdateCheckMode.OnlyAtStart
        ),
        new(LangKeys.Pages_Settings_Software_Update_Auto_Check_Update_Mode_Selector_At_Start_And_Background,
            AppUpdateCheckMode.AtStartAndBackground
        )
    ];

    public bool DownloadUpdateAtBackground
    {
        get => appSettings.Value.DownloadUpdateAtBackground;
        set
        {
            OnPropertyChanging();
            appSettings.Update(s => s.DownloadUpdateAtBackground = value);
            OnPropertyChanged();
        }
    }

    public bool ReceivePreReleaseUpdate
    {
        get => appSettings.Value.ReceivePreviewUpdate;
        set
        {
            OnPropertyChanging();
            appSettings.Update(s => s.ReceivePreviewUpdate = value);
            OnPropertyChanged();
        }
    }

    [ObservableProperty] public partial string? StatusText { get; private set; }

    [RelayCommand]
    private async Task ConfirmInstallUpdate()
    {
        if (appUpdateService.UpdateInformation is null)
            return;

        await dialogService.ShowDialogAsync(availableDialogFactory.Create(appUpdateService.UpdateInformation));
    }

    [RelayCommand]
    private async Task ClearVersionSkipAndCheckForUpdate()
    {
        await appSettings.UpdateAsync(s => s.SkipVersion = null);
        await CheckForUpdate();
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        try
        {
            var update = await appUpdateCheckService.CheckForUpdateAsync();

            if (update is null)
            {
                StatusText = LangKeys.Pages_Settings_Software_Update_Check_For_Status_Up_To_Dated;
                return;
            }

            StatusText = null;
            await dialogService.ShowDialogAsync(availableDialogFactory.Create(update));
        }
        catch (Exception ex)
        {
            StatusText =
                string.Format(
                    I18NExtension.Translate(LangKeys
                        .Pages_Settings_Software_Update_Check_For_Status_Failed_To_Check_Update_Template) ??
                    "Failed to check for update: {0}"
                    , ex.Message
                );
        }
    }

    #endregion
}

public sealed record UpdateSettingsUpdateCheckModeViewModel(string UpdateModeText, AppUpdateCheckMode Mode);