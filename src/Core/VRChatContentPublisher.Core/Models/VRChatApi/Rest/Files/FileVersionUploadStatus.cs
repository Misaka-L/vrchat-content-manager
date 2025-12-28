using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Files;

public record FileVersionUploadStatus(
    [property: JsonPropertyName("nextPartNumber")] int NextPartNumber
);