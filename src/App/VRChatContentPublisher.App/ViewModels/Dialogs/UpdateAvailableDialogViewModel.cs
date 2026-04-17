using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class UpdateAvailableDialogViewModel(
    AppUpdateInformation updateInformation,
    IWritableOptions<AppSettings> appSettings
) : DialogViewModelBase
{
    public string Version => updateInformation.Version;
    public string Notes => updateInformation.Notes;
    public DateTimeOffset ReleaseDate => updateInformation.ReleaseDate;

    [RelayCommand]
    private async Task MarkVersionAsSkippedAsync()
    {
        await appSettings.UpdateAsync(s => s.SkipVersion = updateInformation.Version);
        RequestClose();
    }
}

public sealed class ConfirmUpdateDialogViewModelFactory(
    IWritableOptions<AppSettings> appSettings
)
{
    public UpdateAvailableDialogViewModel Create(AppUpdateInformation updateInformation)
    {
        return new UpdateAvailableDialogViewModel(updateInformation, appSettings);
    }
}