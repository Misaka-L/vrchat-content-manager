using System.Text.Json.Serialization;

namespace VRChatContentPublisher.Core.Settings.Models;

[JsonSerializable(typeof(UserSessionStorage))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(RpcSessionStorage))]
[JsonSerializable(typeof(IpCryptStorage))]
[JsonSerializable(typeof(PublicIpStateStorage))]
public partial class SettingsJsonContext : JsonSerializerContext;