using System.Diagnostics;
using OpenTelemetry;
using VRChatContentPublisher.TelemetryCore.Extensions;

namespace VRChatContentPublisher.TelemetryCore;

public class EnvironmentTagProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        data.SetTag("environment", SentrySdkExtension.GetEnvironment());
    }
}