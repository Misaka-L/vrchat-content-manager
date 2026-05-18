using System.Text.Json.Serialization;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher.Options;

namespace VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;

[JsonSerializable(typeof(ContentPublishTaskState))]
[JsonSerializable(typeof(WorldContentPublisherOptions))]
[JsonSerializable(typeof(AvatarContentPublisherOptions))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ContentPublishTaskStateJsonContext : JsonSerializerContext;
