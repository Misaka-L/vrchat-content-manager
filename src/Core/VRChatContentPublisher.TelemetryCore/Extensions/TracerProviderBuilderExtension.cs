using OpenTelemetry.Trace;
using VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry;
using VRChatContentPublisher.TelemetryCore.OpenTelemetryProcessor;
using VRChatContentPublisher.TelemetryCore.TelemetryToggle;

namespace VRChatContentPublisher.TelemetryCore.Extensions;

public static class TracerProviderBuilderExtension
{
    public static TracerProviderBuilder AddAppSentryExporter(
        this TracerProviderBuilder builder,
        string appSessionLifetimeId)
    {
        builder.SetSampler(new AppOpenTelemetryToggleSampler());

        builder.AddProcessor(new CustomTagsProcessor([
            new CustomTagsProcessorTag("app.lifetime_session_id", appSessionLifetimeId),
            new CustomTagsProcessorTag("user.id", InstallationIdProvider.GetInstallationId())
        ]));

        builder.AddProcessor(new EnvironmentTagProcessor());
        builder.AddProcessor(new OpenTelemetryMaskingProcessor());
        builder.AddSentryOtlpExporter(SentryDsnProvider.GetDsn());

        return builder;
    }
}