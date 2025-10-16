﻿using System.Text.Json.Serialization;
using VRChatContentManager.Core.Models.VRChatApi.Rest.UnityPackages;

namespace VRChatContentManager.Core.Models.VRChatApi.Rest.Avatars;

public record VRChatApiAvatar(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unityPackages")]
    VRChatApiUnityPackage[] UnityPackages,
    [property: JsonPropertyName("authorId")]
    string AuthorId
);