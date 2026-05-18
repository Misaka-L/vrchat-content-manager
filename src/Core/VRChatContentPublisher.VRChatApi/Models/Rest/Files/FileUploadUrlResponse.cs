using System.Text.Json.Serialization;

namespace VRChatContentPublisher.VRChatApi.Models.Rest.Files;

public record FileUploadUrlResponse(
    [property: JsonPropertyName("url")] string Url
);