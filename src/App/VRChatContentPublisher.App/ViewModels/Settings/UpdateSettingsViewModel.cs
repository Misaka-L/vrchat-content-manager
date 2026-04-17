using Antelcat.I18N.Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services.Update;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class UpdateSettingsViewModel(
    IWritableOptions<AppSettings> appSettings,
    AppUpdateCheckService appUpdateCheckService
) : ViewModelBase
{
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
    private async Task CheckForUpdate()
    {
        try
        {
            var update = await appUpdateCheckService.CheckForUpdateAsync(true);

            // Update Check Service will sned in-app notification and show dialog
            if (update.Version != AppVersionUtils.GetAppVersion())
                return;

            StatusText = LangKeys.Pages_Settings_Software_Update_Check_For_Status_Up_To_Dated;
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
}

public sealed record UpdateSettingsUpdateCheckModeViewModel(string UpdateModeText, AppUpdateCheckMode Mode);