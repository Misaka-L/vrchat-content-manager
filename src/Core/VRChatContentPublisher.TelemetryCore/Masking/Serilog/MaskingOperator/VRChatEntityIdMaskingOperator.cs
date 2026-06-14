using System.Text.RegularExpressions;
using Serilog.Enrichers.Sensitive;

namespace VRChatContentPublisher.TelemetryCore.Masking.Serilog.MaskingOperator;

public class VRChatEntityIdMaskingOperator()
    : RegexMaskingOperator(
        @"(usr|avtr|wrld|file)_([0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12})")
{
    protected override bool ShouldMaskInput(string input)
    {
        return input.Contains("usr_") || input.Contains("avtr_") || input.Contains("wrld_") || input.Contains("file_");
    }

    protected override string PreprocessMask(string mask, Match match)
    {
        return match.Groups[1].Value + "_" + MaskingHelper.Hash(match.Groups[2].Value);
    }
}