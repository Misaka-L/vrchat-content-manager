using VRChatContentManager.Core.Models;

namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public interface IContentPublisher
{
    event EventHandler<PublishTaskProgressEventArg> ProgressChanged;

    string GetContentType();
    string GetContentName();
    string GetContentPlatform();
    ValueTask PublishAsync(Stream bundleFileStream, HttpClient awsClient, CancellationToken cancellationToken = default);
}