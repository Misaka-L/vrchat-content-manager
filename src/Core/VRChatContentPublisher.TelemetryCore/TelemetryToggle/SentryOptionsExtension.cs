namespace VRChatContentPublisher.TelemetryCore.TelemetryToggle;

public static class SentryOptionsExtension
{
    extension(SentryOptions sentryOptions)
    {
        public void AddTelemetryModeListener()
        {
            // For how masking (Privacy Mode) works, see Masking namespace
            sentryOptions.SetBeforeSend(evt =>
            {
                if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
                {
                    return null;
                }

                return evt;
            });

            // For how we mask logging, see:
            // VRChatContentPublisher.TelemetryCore.Masking.Serilog.SensitiveDataEnricherOptionsExtension
            sentryOptions.SetBeforeSendLog(log =>
            {
                if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
                {
                    return null;
                }

                return log;
            });

            // Currently, we just let sentry generate breadcrumbs from logging, so see logging masking.
            sentryOptions.SetBeforeBreadcrumb(breadcrumb =>
            {
                if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
                {
                    return null;
                }

                return breadcrumb;
            });

            // Actually we don't use sentry transaction,
            // we use OpenTelemetry for tracing and send information to Sentry OTLP endpoint.
            // See VRChatContentPublisher.TelemetryCore.Extensions.TracerProviderBuilderExtension.AddAppSentryExporter

            // For how we mask OpenTelemetry tracing, see:
            // VRChatContentPublisher.TelemetryCore.Masking.OpenTelemetry.OpenTelemetryMaskingProcessor
            sentryOptions.SetBeforeSendTransaction(transaction =>
            {
                if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
                {
                    return null;
                }

                return transaction;
            });

            // Currently, not metric was used in app
            sentryOptions.SetBeforeSendMetric(metric =>
            {
                if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
                {
                    return null;
                }

                return metric;
            });
        }
    }
}