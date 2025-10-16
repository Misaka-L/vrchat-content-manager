using System.Text.Json.Serialization;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Avatars;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Files;
using VRChatContentManager.Core.Models.VRChatApi.Rest.UnityPackages;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Worlds;

namespace VRChatContentManager.Core.Models.VRChatApi.Rest;

[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(RequireTwoFactorAuthResponse))]
[JsonSerializable(typeof(VerifyTotpRequest))]
[JsonSerializable(typeof(CurrentUser))]
[JsonSerializable(typeof(VRChatApiFile))]
[JsonSerializable(typeof(VRChatApiFileVersion))]
[JsonSerializable(typeof(CreateFileVersionRequest))]
[JsonSerializable(typeof(FileVersionUploadStatus))]
[JsonSerializable(typeof(FileUploadUrlResponse))]
[JsonSerializable(typeof(CompleteFileUploadRequest))]
[JsonSerializable(typeof(VRChatApiWorld))]
[JsonSerializable(typeof(VRChatApiUnityPackage))]
[JsonSerializable(typeof(CreateWorldVersionRequest))]
[JsonSerializable(typeof(VRChatApiAvatar))]
[JsonSerializable(typeof(CreateAvatarVersionRequest))]
[JsonSerializable(typeof(Requires2FA))]
public sealed partial class ApiJsonContext : JsonSerializerContext;