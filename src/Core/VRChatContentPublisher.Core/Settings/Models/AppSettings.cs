using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class AppSettings
{
    public bool SkipFirstSetup { get; set; } = false;
    public bool UseRgbCyclingBackgroundMenu { get; set; } = false;
    public string ConnectInstanceName { get; set; } = RandomWordsUtils.GetRandomWords();

    public AppHttpProxySettings HttpProxySettings { get; set; } = AppHttpProxySettings.SystemProxy;
    public Uri? HttpProxyUri { get; set; }

    public AppTasksPageSortMode TasksPageSortMode { get; set; } = AppTasksPageSortMode.LatestFirst;
}

public enum AppHttpProxySettings
{
    NoProxy = 0,
    SystemProxy = 1,
    CustomProxy = 2
}

public enum AppTasksPageSortMode
{
    LatestFirst = 0,
    OldestFirst = 1,
}