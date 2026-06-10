using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using VRChatContentPublisher.BundleProcessCore.Telemetry;
using VRChatContentPublisher.ConnectCore.Telemetry;
using VRChatContentPublisher.Core.Telemetry;
using VRChatContentPublisher.PersistentCore.Telemetry;
using VRChatContentPublisher.VRChatApi.Telemetry;

namespace VRChatContentPublisher.App.Extensions;

public static class HostBuilderExtension
{
    public static T AddAppTelemetry<T>(this T builder) where T : IHostApplicationBuilder
    {
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

                tracing.AddSentry();
            })
            .WithMetrics(metrics => { metrics.AddHttpClientInstrumentation(); });

        return builder;
    }
}