using System.Text.Json.Serialization;

namespace VRChatContentPublisher.VRChatApi.Models.AtlassianStatusPage;

[JsonSerializable(typeof(StatusPageSummary))]
public partial class StatusPageJsonContext : JsonSerializerContext;