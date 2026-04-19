using System.Text.Json.Serialization;

namespace VRChatContentPublisher.App.Models.Update;

public sealed record AppUpdateInformation(
    [property: JsonPropertyName("version")]
    string Version,
    [property: JsonPropertyName("notes")] string Notes,
    [property: JsonPropertyName("browserUrl")]
    string BrowserUrl,
    [property: JsonPropertyName("releaseDate")]
    DateTimeOffset ReleaseDate,
    [property: JsonPropertyName("platforms")]
    Dictionary<string, AppUpdatePlatformInformation> Platforms
);

public sealed record AppUpdatePlatformInformation(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("sha256")] string Sha256
);

[JsonSerializable(typeof(AppUpdateInformation))]
public sealed partial class AppUpdateInformationJsonContext : JsonSerializerContext;