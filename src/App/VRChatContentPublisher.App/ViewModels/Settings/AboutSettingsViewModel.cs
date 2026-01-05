using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class AboutSettingsViewModel(NavigationService navigationService) : ViewModelBase
{
    public string AppVersion => AppVersionUtils.GetAppVersion();
    public string AppCommitHash => AppVersionUtils.GetAppCommitHash();
    public DateTimeOffset? AppBuildDate => AppVersionUtils.GetAppBuildDate()?.ToLocalTime();

    public const string ThirdPartNoticeRelativePath = "THIRD-PARTY-NOTICES.TXT";
    public string ThirdPartNoticePath => Path.GetFullPath(ThirdPartNoticeRelativePath);

    [RelayCommand]
    private void NavigateToLicense()
    {
        navigationService.Navigate<LicensePageViewModel>();
    }
}