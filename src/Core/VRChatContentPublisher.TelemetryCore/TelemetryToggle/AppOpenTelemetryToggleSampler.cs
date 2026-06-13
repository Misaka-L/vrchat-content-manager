using OpenTelemetry.Trace;

namespace VRChatContentPublisher.TelemetryCore.TelemetryToggle;

public class AppOpenTelemetryToggleSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // For how masking (Privacy Mode) works, see Masking namespace
        if (TelemetrySettings.TelemetryMode == TelemetryMode.Disabled)
            return new SamplingResult(SamplingDecision.RecordAndSample);

        return new SamplingResult(SamplingDecision.Drop);
    }
}