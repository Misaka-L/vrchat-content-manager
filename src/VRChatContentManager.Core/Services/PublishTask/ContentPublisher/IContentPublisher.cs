using VRChatContentManager.Core.Models;

namespace VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

public interface IContentPublisher
{
    event EventHandler<PublishTaskProgressEventArg> ProgressChanged;
    
    ValueTask PublishAsync(Stream bundleFileStream, HttpClient awsClient);
}