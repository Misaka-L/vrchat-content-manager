using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class AppSettings
{
    public bool SkipFirstSetup { get; set; } = false;
    public string ConnectInstanceName { get; set; } = RandomWordsUtils.GetRandomWords();

    public AppHttpProxySettings HttpProxySettings { get; set; } = AppHttpProxySettings.SystemProxy;
    public Uri? HttpProxyUri { get; set; }
}

public enum AppHttpProxySettings
{
    NoProxy = 0,
    SystemProxy = 1,
    CustomProxy = 2
}