using System.Text.Json.Serialization;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.UnityPackages;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Avatars;

public record VRChatApiAvatar(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unityPackages")]
    VRChatApiUnityPackage[] UnityPackages,
    [property: JsonPropertyName("authorId")]
    string AuthorId,
    [property: JsonPropertyName("imageUrl")]
    string? ImageUrl = null
);