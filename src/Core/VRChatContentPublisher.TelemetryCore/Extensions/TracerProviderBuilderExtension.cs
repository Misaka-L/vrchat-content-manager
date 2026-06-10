using OpenTelemetry.Trace;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class TracerProviderBuilderExtension
{
    public static TracerProviderBuilder AddAppSentryExporter(this TracerProviderBuilder builder)
    {
        builder.AddProcessor(new EnvironmentTagProcessor());
        builder.AddSentryOtlpExporter(SentryDsnProvider.GetDsn());

        return builder;
    }
}