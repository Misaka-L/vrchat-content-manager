using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;

public record CurrentUser(
    [property: JsonPropertyName("username")] string UserName,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("currentAvatarThumbnailImageUrl")] string AvatarThumbnailImageUrl,
    [property: JsonPropertyName("profilePicOverrideThumbnail")] string? ProfilePictureThumbnailUrl = null
);