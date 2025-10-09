using System.Text.Json.Serialization;
using VRChatContentManager.ConnectCore.Models.Api.V1.Responses.Meta;

namespace VRChatContentManager.ConnectCore.Models.Api.V1;

[JsonSerializable(typeof(ApiV1MetadataResponse))]
public sealed partial class ApiV1JsonContext : JsonSerializerContext;