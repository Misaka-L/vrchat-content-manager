using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using VRChatContentPublisher.BundleProcessCore.Telemetry;
using VRChatContentPublisher.ConnectCore.Telemetry;
using VRChatContentPublisher.Core.Telemetry;
using VRChatContentPublisher.PersistentCore.Telemetry;
using VRChatContentPublisher.TelemetryCore;
using VRChatContentPublisher.TelemetryCore.Extensions;
using VRChatContentPublisher.VRChatApi.Telemetry;

namespace VRChatContentPublisher.App.Extensions;

public static class HostBuilderExtension
{
    public static T AddAppTelemetry<T>(
        this T builder,
        string appSessionLifetimeId
    ) where T : IHostApplicationBuilder
    {
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/d141db4406ed556e8c57cdef6d7a09bf51295bf4/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/Implementation/ExperimentalOptions.cs#L36-L48
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/1fafead47395a517ff827d61cef731052849f90f/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptionsExtensions.cs#L104-L116
            { "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "disk" },
            { "OTEL_DOTNET_EXPERIMENTAL_OTLP_DISK_RETRY_DIRECTORY_PATH", TelemetryConst.GetOtlpDiskRetryPath() }
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddHttpClientInstrumentation();
                tracing.AddSource(CoreActivitySources.ContentPublishingActivitySourceName);
                tracing.AddSource(CoreActivitySources.RpcActivitySourceName);
                tracing.AddSource(SqliteCoreActivitySources.SqliteCoreActivitySourceName);
                tracing.AddSource(VRChatApiCoreActivitySources.VRChatApiActivitySourceName);
                tracing.AddSource(BundleProcessCoreActivitySources.BundleProcessCoreActivitySourceName);
                tracing.AddSource(ConnectCoreActivitySources.ConnectCoreSourceName);

                // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-builtin-activities
                tracing.AddSource("Experimental.System.Net.Http.Connections");
                tracing.AddSource("Experimental.System.Net.Http.Connections");
                tracing.AddSource("Experimental.System.Net.NameResolution");
                tracing.AddSource("Experimental.System.Net.Sockets");
                tracing.AddSource("Experimental.System.Net.Security");

                tracing.AddOtlpExporter();
                tracing.AddAppSentryExporter(appSessionLifetimeId);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddHttpClientInstrumentation();
                metrics.AddOtlpExporter();
            });

        return builder;
    }
}