using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Settings.Models;

[JsonSerializable(typeof(UserSessionStorage))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(RpcSessionStorage))]
public partial class SettingsJsonContext : JsonSerializerContext;