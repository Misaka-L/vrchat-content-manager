using System.Net;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;

namespace VRChatContentPublisher.Core.Settings.Models;

public sealed class UserSessionStorage
{
    public Dictionary<string, UserSessionStorageItem> Sessions { get; set; } = new();
}

public sealed record UserSessionStorageItem(
    string UserName,
    List<Cookie>? Cookies = null,
    UserSessionUserInfo? User = null)
{
    public UserSessionUserInfo? User { get; set; } = User;

    public CurrentUser GetApiUserModel()
    {
        return new CurrentUser(
            UserName,
            User?.DisplayName ?? UserName,
            User?.Id ?? UserName,
            User?.AvatarThumbnailImageUrl ?? string.Empty,
            User?.ProfilePictureThumbnailUrl
        );
    }
}

public sealed record UserSessionUserInfo(
    string Id,
    string DisplayName,
    string? AvatarThumbnailImageUrl = null,
    string? ProfilePictureThumbnailUrl = null)
{
    public static UserSessionUserInfo Create(CurrentUser currentUser)
    {
        return new UserSessionUserInfo(
            currentUser.Id,
            currentUser.DisplayName,
            currentUser.AvatarThumbnailImageUrl,
            currentUser.ProfilePictureThumbnailUrl
        );
    }
}