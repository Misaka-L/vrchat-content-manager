using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.Core.Shared;

namespace VRChatContentPublisher.Core.Settings;

// https://stackoverflow.com/a/42705862
public sealed class WritableOptions<T>(
    string sectionName,
    OptionsWriter writer,
    IOptionsMonitor<T> options)
    : IWritableOptions<T>
    where T : class, new()
{
    public T Value => options.CurrentValue;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public void Update(Action<T> applyChanges)
    {
        using (SimpleSemaphoreSlimLockScope.Wait(_semaphoreSlim))
        {
            writer.UpdateOptions(opt =>
            {
                var jsonTypeInfo = SettingsJsonContext.Default.GetTypeInfo(typeof(T));
                if (jsonTypeInfo is null)
                    throw new InvalidOperationException($"No Json type info for type {typeof(T)}");

                T sectionObject;
                if (opt[sectionName] is { } sectionNode)
                {
                    sectionObject = sectionNode.Deserialize(jsonTypeInfo) as T ?? new T();
                }
                else
                {
                    sectionObject = new T();
                }

                applyChanges(sectionObject);

                var json = JsonSerializer.Serialize(sectionObject, jsonTypeInfo);
                opt[sectionName] = JsonNode.Parse(json);
            });
        }
    }

    public async Task UpdateAsync(Action<T> applyChanges)
    {
        using (await SimpleSemaphoreSlimLockScope.WaitAsync(_semaphoreSlim))
        {
            await writer.UpdateOptionsAsync(opt =>
            {
                var jsonTypeInfo = SettingsJsonContext.Default.GetTypeInfo(typeof(T));
                if (jsonTypeInfo is null)
                    throw new InvalidOperationException($"No Json type info for type {typeof(T)}");

                T sectionObject;
                if (opt[sectionName] is { } sectionNode)
                {
                    sectionObject = sectionNode.Deserialize(jsonTypeInfo) as T ?? new T();
                }
                else
                {
                    sectionObject = new T();
                }

                applyChanges(sectionObject);

                var json = JsonSerializer.Serialize(sectionObject, jsonTypeInfo);
                opt[sectionName] = JsonNode.Parse(json);
            });
        }
    }
}