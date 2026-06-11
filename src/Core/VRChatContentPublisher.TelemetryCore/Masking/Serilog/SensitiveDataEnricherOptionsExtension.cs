using Serilog.Enrichers.Sensitive;
using VRChatContentPublisher.TelemetryCore.Masking.Serilog.MaskingOperator;

namespace VRChatContentPublisher.TelemetryCore.Masking.Serilog;

public static class SensitiveDataEnricherOptionsExtension
{
    public static SensitiveDataEnricherOptions AddAppMaskingOptions(this SensitiveDataEnricherOptions options)
    {
        options.MaskingOperators =
        [
            new EmailAddressMaskingOperator(),
            new VRChatEntityIdMaskingOperator()
        ];

        options.MaskProperties.Add(new MaskProperty
        {
            Name = "UserName"
        });
        options.MaskProperties.Add(new MaskProperty
        {
            Name = "ContentName"
        });
        options.MaskProperties.Add(new MaskProperty
        {
            Name = "ClientName"
        });

        options.MaskValue = MaskingConst.MaskedValue;

        return options;
    }
}