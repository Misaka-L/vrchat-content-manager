using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.VRChatApi.Models.Rest.Worlds;

public record CreateWorldRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("assetUrl")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? AssetUrl,
    [property: JsonPropertyName("assetVersion")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? AssetVersion,
    [property: JsonPropertyName("platform")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Platform,
    [property: JsonPropertyName("unityVersion")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UnityVersion,
    [property: JsonPropertyName("worldSignature")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? WorldSignature,
    [property: JsonPropertyName("imageUrl")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ImageUrl,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Description,
    [property: JsonPropertyName("tags")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string[]? Tags,
    [property: JsonPropertyName("releaseStatus")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ReleaseStatus,
    [property: JsonPropertyName("capacity")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? Capacity,
    [property: JsonPropertyName("recommendedCapacity")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? RecommendedCapacity,
    [property: JsonPropertyName("previewYoutubeId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PreviewYoutubeId,
    [property: JsonPropertyName("udonProducts")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string[]? UdonProducts
);