using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Files;

public record FileUploadUrlResponse(
    [property: JsonPropertyName("url")] string Url
);