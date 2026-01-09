using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.AtlassianStatusPage;

public sealed record StatusPageSummary(
    [property: JsonPropertyName("components")]
    StatusPageComponent[] Components,
    [property: JsonPropertyName("status")] StatusPageSummaryStatus Status
);

public sealed record StatusPageSummaryStatus(
    [property: JsonPropertyName("indicator")]
    string Indicator,
    [property: JsonPropertyName("description")]
    string Description
);

public sealed record StatusPageComponent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("group_id")]
    string? GroupId,
    [property: JsonPropertyName("group")] bool IsGroup
);