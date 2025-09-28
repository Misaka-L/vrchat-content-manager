using System.Text.Json.Serialization;

namespace VRChatContentManager.Core.Settings.Models;

[JsonSerializable(typeof(UserSessionStorage))]
public partial class SettingsJsonContext : JsonSerializerContext;