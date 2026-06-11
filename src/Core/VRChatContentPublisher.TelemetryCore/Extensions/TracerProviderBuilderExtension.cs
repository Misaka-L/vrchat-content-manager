using OpenTelemetry.Trace;
using VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class TracerProviderBuilderExtension
{
    public static TracerProviderBuilder AddAppSentryExporter(this TracerProviderBuilder builder)
    {
        builder.AddProcessor(new EnvironmentTagProcessor());
        builder.AddProcessor(new OpenTelemetryMaskingProcessor());
        builder.AddSentryOtlpExporter(SentryDsnProvider.GetDsn());

        return builder;
    }
}