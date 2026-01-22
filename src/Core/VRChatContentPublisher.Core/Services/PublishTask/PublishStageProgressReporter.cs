using VRChatContentPublisher.BundleProcessCore.Services;

namespace VRChatContentPublisher.Core.Services.PublishTask;

public sealed class PublishStageProgressReporter(Action<string, double?> progressReporter) : IProcessProgressReporter
{
    public void Report(string progressText, double? progressValue = null)
    {
        progressReporter(progressText, progressValue);
    }
}