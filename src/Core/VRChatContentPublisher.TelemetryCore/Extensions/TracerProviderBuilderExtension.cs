using OpenTelemetry.Trace;
using VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry;
using VRChatContentPublisher.TelemetryCore.TelemetryToggle;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class TracerProviderBuilderExtension
{
    public static TracerProviderBuilder AddAppSentryExporter(this TracerProviderBuilder builder)
    {
        builder.SetSampler(new AppOpenTelemetryToggleSampler());

        builder.AddProcessor(new EnvironmentTagProcessor());
        builder.AddProcessor(new OpenTelemetryMaskingProcessor());
        builder.AddSentryOtlpExporter(SentryDsnProvider.GetDsn());

        return builder;
    }
}