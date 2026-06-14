using System.Text.RegularExpressions;

namespace VRChatContentPublisher.TelemetryCore.Masking;

public static partial class MaskingRegex
{
    public const string VRChatEntityIdPattern =
        @"(usr|avtr|wrld|file)_([0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12})";

    [GeneratedRegex(VRChatEntityIdPattern)]
    public static partial Regex VRChatEntityIdRegex { get; }
}