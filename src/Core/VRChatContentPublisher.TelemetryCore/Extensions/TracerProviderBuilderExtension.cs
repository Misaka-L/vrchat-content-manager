using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry;
using VRChatContentPublisher.TelemetryCore.OpenTelemetryProcessor;
using VRChatContentPublisher.TelemetryCore.TelemetryToggle;
using VRChatContentPublisher.TelemetryCore.Utils;

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
        builder.AddAppSentryOtlpExporter();

        return builder;
    }

    // https://github.com/getsentry/sentry-dotnet/blob/4b51c11082639a3fe4492e1467031fe9c85ec541/src/Sentry.OpenTelemetry.Exporter/SentryTracerProviderBuilderExtensions.cs#L42-L77
    private static TracerProviderBuilder AddAppSentryOtlpExporter(
        this TracerProviderBuilder builder
    )
    {
        if (SentryDsnProvider.TryGetParsedDsn() is not { } dsn)
        {
            return builder;
        }

        var defaultTextMapPropagator = new SentryPropagator();
        Sdk.SetDefaultTextMapPropagator(defaultTextMapPropagator);

        var collectorUrl = dsn.GetOtlpTracesEndpointUri();
        builder.AddOtlpExporter(options =>
        {
            options.Endpoint = collectorUrl;
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.HttpClientFactory = () =>
            {
                var client = new HttpClient();
                var sdkVersion = SentrySdkVersionProvider.GetSdkVersion();
                client.DefaultRequestHeaders.Add("X-Sentry-Auth",
                    $"Sentry sentry_version={SentryConstants.ProtocolVersion}," +
                    $"sentry_client={sdkVersion.Name}/{sdkVersion.Version}," +
                    $"sentry_key={dsn.PublicKey}");
                return client;
            };
        });

        return builder;
    }
}