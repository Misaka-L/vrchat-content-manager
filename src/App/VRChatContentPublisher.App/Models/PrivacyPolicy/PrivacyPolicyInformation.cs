using System.Text.Json.Serialization;

namespace VRChatContentPublisher.App.Models.PrivacyPolicy;

public sealed record PrivacyPolicyApiResponse(
    [property: JsonPropertyName("privacyPolicy")]
    PrivacyPolicyData PrivacyPolicy
);

public sealed record PrivacyPolicyData(
    [property: JsonPropertyName("version")]
    int Version,
    [property: JsonPropertyName("url")]
    string Url,
    [property: JsonPropertyName("language")]
    Dictionary<string, string>? Language
);

[JsonSerializable(typeof(PrivacyPolicyApiResponse))]
public sealed partial class PrivacyPolicyJsonContext : JsonSerializerContext;
