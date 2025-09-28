using System.Text.Json.Serialization;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Auth;

namespace VRChatContentManager.Core.Models.VRChatApi.Rest;

[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(RequireTwoFactorAuthResponse))]
[JsonSerializable(typeof(VerifyTotpRequest))]
[JsonSerializable(typeof(CurrentUser))]
[JsonSerializable(typeof(Requires2FA))]
public sealed partial class ApiJsonContext : JsonSerializerContext;