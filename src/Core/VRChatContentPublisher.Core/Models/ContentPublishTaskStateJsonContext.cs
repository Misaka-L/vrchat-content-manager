using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models;

[JsonSerializable(typeof(ContentPublishTaskState))]
[JsonSerializable(typeof(WorldContentPublisherOptions))]
[JsonSerializable(typeof(AvatarContentPublisherOptions))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ContentPublishTaskStateJsonContext : JsonSerializerContext;
