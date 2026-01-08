using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;

public record CurrentUser(
    [property: JsonPropertyName("username")]
    string UserName,
    [property: JsonPropertyName("displayName")]
    string DisplayName,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("currentAvatarThumbnailImageUrl")]
    string AvatarThumbnailImageUrl,
    [property: JsonPropertyName("profilePicOverrideThumbnail")]
    string? ProfilePictureThumbnailUrl = null,
    [property: JsonPropertyName("developerType")]
    string? DeveloperType = null,
    [property: JsonPropertyName("tags")] string[]? Tags = null
)
{
    public bool CanPublishWorld() => DeveloperType == "internal" || Tags?.Contains("system_world_access") == true;
    public bool CanPublishAvatar() => DeveloperType == "internal" || Tags?.Contains("system_avatar_access") == true;
}