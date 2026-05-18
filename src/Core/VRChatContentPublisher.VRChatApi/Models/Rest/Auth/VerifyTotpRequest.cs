using System.Text.Json.Serialization;

namespace VRChatContentPublisher.VRChatApi.Models.Rest.Auth;

public record VerifyTotpRequest(
    [property: JsonPropertyName("code")] string Code
);