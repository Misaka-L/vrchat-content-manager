using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;

public record VerifyTotpRequest(
    [property: JsonPropertyName("code")] string Code
);