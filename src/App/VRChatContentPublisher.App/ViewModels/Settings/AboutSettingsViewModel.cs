using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed class AboutSettingsViewModel : ViewModelBase
{
    public string AppVersion => AppVersionUtils.GetAppVersion();
    public string AppCommitHash => AppVersionUtils.GetAppCommitHash();
    public DateTimeOffset? AppBuildDate => AppVersionUtils.GetAppBuildDate();
}