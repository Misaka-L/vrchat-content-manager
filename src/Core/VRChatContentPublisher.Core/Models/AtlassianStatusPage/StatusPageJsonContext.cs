using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.AtlassianStatusPage;

[JsonSerializable(typeof(StatusPageSummary))]
public partial class StatusPageJsonContext : JsonSerializerContext;