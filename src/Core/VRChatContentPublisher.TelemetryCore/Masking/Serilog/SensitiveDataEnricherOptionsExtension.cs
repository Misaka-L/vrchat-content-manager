using Serilog;
using Serilog.Configuration;
using Serilog.Enrichers.Sensitive;
using VRChatContentPublisher.TelemetryCore.Masking.Serilog.MaskingOperator;

namespace VRChatContentPublisher.TelemetryCore.Masking.Serilog;

public static class SensitiveDataEnricherOptionsExtension
{
    public static LoggerConfiguration WithAppSensitiveDataMasking(
        this LoggerEnrichmentConfiguration enrichmentConfiguration
    )
    {
        return enrichmentConfiguration.When(_ => TelemetrySettings.TelemetryMode != TelemetryMode.All,
            enrichConfig =>
                enrichConfig
                    .WithSensitiveDataMasking(options =>
                        options.AddAppMaskingOptions()));
    }

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
        options.MaskProperties.Add(new MaskProperty
        {
            Name = "OldIp"
        });
        options.MaskProperties.Add(new MaskProperty
        {
            Name = "NewIp"
        });

        options.MaskValue = MaskingConst.MaskedValue;

        return options;
    }
}