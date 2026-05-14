using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using VRChatContentPublisher.App.Localization;
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

    public record EarlySupporter(string Name, string? Url = null);

    public IReadOnlyList<EarlySupporter> EarlySupporters { get; } =
    [
        new("@小川糸"),
        new("@ColorlessColor", "https://github.com/ColorlessColor"),
        new("@flower-elf", "https://github.com/flower-elf"),
        new("@Anteness", "https://github.com/Anteness"),
        new("@SKPloft", "https://github.com/SKPloft"),
        new("@lonelyicer", "https://github.com/lonelyicer"),
    ];

    public record SupportAction(string Label, MaterialIconKind IconKind, string IconColor, string Url);

    public IReadOnlyList<SupportAction> SupportActions { get; } =
    [
        new(LangKeys.Pages_Settings_About_Consider_Support_By_Content_Donate, MaterialIconKind.Charity, "IndianRed",
            "https://afdian.com/a/Misaka-L"),
        new(LangKeys.Pages_Settings_About_Consider_Support_By_Content_Feedback, MaterialIconKind.MessageAlert,
            "Goldenrod", "https://github.com/project-vrcz/content-publisher/issues/new/choose"),
        new(LangKeys.Pages_Settings_About_Consider_Support_By_Content_Contribute, MaterialIconKind.SourceBranch,
            "MediumSeaGreen", "https://github.com/project-vrcz/content-publisher/blob/main/CONTRIBUTING.md"),
    ];

    [RelayCommand]
    private void NavigateToLicense()
    {
        navigationService.Navigate<LicensePageViewModel>();
    }
}