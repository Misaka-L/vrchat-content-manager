using System.Diagnostics;
using OpenTelemetry;

namespace VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry;

public class OpenTelemetryMaskingProcessor : BaseProcessor<Activity>
{
    private static readonly string[] TagToFilter =
    [
        "content.name"
    ];

    public override void OnEnd(Activity data)
    {
        var dirtyList = new List<KeyValuePair<string, string>>();

        foreach (var tagItem in data.Tags)
        {
            var tagKey = tagItem.Key;
            var tagValue = tagItem.Value;

            if (tagValue is null)
                continue;

            if (TryMaskVRChatEntityId(tagValue, out var maskedValue))
            {
                dirtyList.Add(new KeyValuePair<string, string>(tagKey, maskedValue));
                continue;
            }

            if (TagToFilter.Contains(tagKey))
            {
                dirtyList.Add(new KeyValuePair<string, string>(tagKey, MaskingConst.MaskedValue));
            }
        }

        foreach (var tag in dirtyList)
        {
            data.SetTag(tag.Key, tag.Value);
        }
    }

    private bool TryMaskVRChatEntityId(string input, out string maskedValue)
    {
        maskedValue = input;

        if (!input.Contains("usr_") &&
            !input.Contains("avtr_") &&
            !input.Contains("wrld_") &&
            !input.Contains("file_")) return false;

        var match = MaskingRegex.VRChatEntityIdRegex.Match(input);
        if (!match.Success) return false;

        maskedValue = match.Groups[1].Value + "_" + MaskingHelper.Hash(match.Groups[2].Value);
        return true;
    }
}