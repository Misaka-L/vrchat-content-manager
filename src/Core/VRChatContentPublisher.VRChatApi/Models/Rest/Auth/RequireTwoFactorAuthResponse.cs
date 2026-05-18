using System.Text.Json.Serialization;

namespace VRChatContentPublisher.VRChatApi.Models.Rest.Auth;

public record RequireTwoFactorAuthResponse(
    [property: JsonPropertyName("requiresTwoFactorAuth")]
    Requires2FA[] RequiresTwoFactorAuth);