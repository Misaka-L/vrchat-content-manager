using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace VRChatContentPublisher.Core.Settings;

// https://stackoverflow.com/a/42705862
public sealed class OptionsWriter(
    IConfigurationRoot configuration,
    string file)
{
    public void UpdateOptions(Action<JsonNode> callback, bool reload = true)
    {
        var jsonRaw = File.Exists(file) ? File.ReadAllText(file) : "{}";
        var config = JsonNode.Parse(jsonRaw);

        if (config is null)
            throw new InvalidOperationException("Could not parse configuration file.");

        callback(config);
        File.WriteAllText(file, config.ToJsonString());

        configuration.Reload();
    }

    public async Task UpdateOptionsAsync(Action<JsonNode> callback, bool reload = true)
    {
        var jsonRaw = File.Exists(file) ? await File.ReadAllTextAsync(file) : "{}";
        var config = JsonNode.Parse(jsonRaw);

        if (config is null)
            throw new InvalidOperationException("Could not parse configuration file.");

        callback(config);
        await File.WriteAllTextAsync(file, config.ToJsonString());

        configuration.Reload();
    }
}