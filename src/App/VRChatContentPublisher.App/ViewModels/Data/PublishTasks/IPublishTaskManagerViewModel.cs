using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public interface IPublishTaskManagerViewModel
{
    UserSessionService UserSessionService { get; }
}