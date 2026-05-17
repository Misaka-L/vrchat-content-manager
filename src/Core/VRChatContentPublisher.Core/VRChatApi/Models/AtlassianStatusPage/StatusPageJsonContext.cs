using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.VRChatApi.Models.AtlassianStatusPage;

[JsonSerializable(typeof(StatusPageSummary))]
public partial class StatusPageJsonContext : JsonSerializerContext;