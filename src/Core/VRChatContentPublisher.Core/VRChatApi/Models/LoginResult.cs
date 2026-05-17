using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.VRChatApi.Models;

public sealed record LoginResult(bool IsSuccess, Requires2FA[] Requires2Fa);

[JsonConverter(typeof(JsonStringEnumConverter<Requires2FA>))]
public enum Requires2FA
{
    Totp,
    Otp,
    EmailOtp
}