namespace VRChatContentPublisher.BundleProcessCore.Services;

public interface IProcessProgressReporter
{
    public void Report(string progressText, double? progressValue = null);
}