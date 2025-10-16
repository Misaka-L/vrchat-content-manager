namespace VRChatContentManager.Core.Models;

public sealed class PublishTaskProgressEventArg(string progressText, double? progressValue) : EventArgs
{
    public string ProgressText => progressText;
    public double? ProgressValue => progressValue;
}