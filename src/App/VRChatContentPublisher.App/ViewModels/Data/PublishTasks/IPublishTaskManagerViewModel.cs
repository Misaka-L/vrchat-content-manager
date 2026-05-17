using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public interface IPublishTaskManagerViewModel
{
    UserSessionService UserSessionService { get; }
}