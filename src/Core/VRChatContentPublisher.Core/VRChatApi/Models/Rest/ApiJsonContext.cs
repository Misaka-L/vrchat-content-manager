using System.Text.Json.Serialization;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Auth;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Avatars;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Files;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.UnityPackages;
using VRChatContentPublisher.Core.VRChatApi.Models.Rest.Worlds;

namespace VRChatContentPublisher.Core.VRChatApi.Models.Rest;

[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(RequireTwoFactorAuthResponse))]
[JsonSerializable(typeof(VerifyTotpRequest))]
[JsonSerializable(typeof(CurrentUser))]
[JsonSerializable(typeof(VRChatApiFile))]
[JsonSerializable(typeof(VRChatApiFileVersion))]
[JsonSerializable(typeof(CreateFileVersionRequest))]
[JsonSerializable(typeof(FileUploadUrlResponse))]
[JsonSerializable(typeof(CompleteFileUploadRequest))]
[JsonSerializable(typeof(VRChatApiWorld))]
[JsonSerializable(typeof(VRChatApiUnityPackage))]
[JsonSerializable(typeof(CreateWorldVersionRequest))]
[JsonSerializable(typeof(VRChatApiAvatar))]
[JsonSerializable(typeof(CreateAvatarVersionRequest))]
[JsonSerializable(typeof(CreateFileRequest))]
[JsonSerializable(typeof(CreateWorldRequest))]
[JsonSerializable(typeof(Requires2FA))]
public sealed partial class ApiJsonContext : JsonSerializerContext;