using System.Text.Json.Serialization;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.UnityPackages;

namespace VRChatContentPublisher.Core.VRChatApi.Models.Rest.Avatars;

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