using System.Net;

namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class UserSessionStorage
{
    public Dictionary<string, UserSessionStorageItem> Sessions { get; set; } = new();
}

public sealed record UserSessionStorageItem(string UserName, List<Cookie> Cookies);