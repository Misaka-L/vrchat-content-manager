using VRChatContentPublisher.App.Models.Update;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class UpdateAvailableDialogViewModel(
    AppUpdateInformation updateInformation
) : DialogViewModelBase
{
    public string Version => updateInformation.Version;
    public string Notes => updateInformation.Notes;
    public DateTimeOffset ReleaseDate => updateInformation.ReleaseDate;
}

public sealed class ConfirmUpdateDialogViewModelFactory
{
    public UpdateAvailableDialogViewModel Create(AppUpdateInformation updateInformation)
    {
        return new UpdateAvailableDialogViewModel(updateInformation);
    }
}