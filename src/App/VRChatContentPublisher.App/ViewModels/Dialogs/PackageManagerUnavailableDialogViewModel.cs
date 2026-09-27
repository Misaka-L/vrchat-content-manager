using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class PackageManagerUnavailableDialogViewModel(string repositoryUrl) : DialogViewModelBase
{
    public string RepositoryUrl { get; } = repositoryUrl;

    [RelayCommand]
    private void Acknowledge()
    {
        RequestClose(true);
    }
}

public sealed class PackageManagerUnavailableDialogViewModelFactory
{
    public PackageManagerUnavailableDialogViewModel Create(string repositoryUrl)
    {
        return new PackageManagerUnavailableDialogViewModel(repositoryUrl);
    }
}
