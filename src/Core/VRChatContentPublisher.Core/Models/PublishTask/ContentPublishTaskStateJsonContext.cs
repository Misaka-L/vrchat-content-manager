using System.Text.Json.Serialization;
using VRChatContentPublisher.Core.Models.PublishTask.ContentPublisher;

namespace VRChatContentPublisher.Core.Models.PublishTask;

[JsonSerializable(typeof(ContentPublishTaskState))]
[JsonSerializable(typeof(WorldContentPublisherOptions))]
[JsonSerializable(typeof(AvatarContentPublisherOptions))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ContentPublishTaskStateJsonContext : JsonSerializerContext;
