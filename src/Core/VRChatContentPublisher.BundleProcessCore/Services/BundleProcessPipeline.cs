namespace VRChatContentPublisher.BundleProcessCore.Services;

internal sealed class BundleProcessPipeline
{
    public async ValueTask<Stream> ProcessAsync(
        Stream bundleStream,
        IProcessProgressReporter? progressReporter,
        CancellationToken cancellationToken = default)
    {
        

        return bundleStream;
    }
}