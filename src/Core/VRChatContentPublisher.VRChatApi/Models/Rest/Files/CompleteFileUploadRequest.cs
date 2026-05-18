using System.Text.Json.Serialization;

namespace VRChatContentPublisher.VRChatApi.Models.Rest.Files;

public record CompleteFileUploadRequest(
    [property: JsonPropertyName("etags")] string[] ETags
);