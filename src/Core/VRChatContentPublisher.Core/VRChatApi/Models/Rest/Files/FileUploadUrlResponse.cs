using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.VRChatApi.Models.Rest.Files;

public record FileUploadUrlResponse(
    [property: JsonPropertyName("url")] string Url
);