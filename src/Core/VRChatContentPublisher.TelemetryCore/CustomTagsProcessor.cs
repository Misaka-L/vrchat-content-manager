using System.Diagnostics;
using OpenTelemetry;

namespace VRChatContentPublisher.TelemetryCore;

public sealed class CustomTagsProcessor(params CustomTagsProcessorTag[] tags) : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        foreach (var (name, value) in tags)
        {
            data.SetTag(name, value);
        }
    }
}

public sealed record CustomTagsProcessorTag(string Name, string Value);