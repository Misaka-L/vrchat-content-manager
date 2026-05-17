using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.VRChatApi.Models.Rest.Files;

public record CompleteFileUploadRequest(
    [property: JsonPropertyName("etags")] string[] ETags
);