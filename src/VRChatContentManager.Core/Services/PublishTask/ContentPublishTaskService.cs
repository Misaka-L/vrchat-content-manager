using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.Core.Services.PublishTask;

public sealed class ContentPublishTaskService
{
    private readonly UserSessionService _userSessionService;
    private readonly HttpClient _awsHttpClient;
    private readonly string _contentId;

    internal ContentPublishTaskService(string contentId, UserSessionService userSessionService,
        HttpClient awsHttpClient)
    {
        _contentId = contentId;
        _userSessionService = userSessionService;
        _awsHttpClient = awsHttpClient;
    }
}

public sealed class ContentPublishTaskFactory(UserSessionService userSessionService, HttpClient awsHttpClient)
{
    public ValueTask<ContentPublishTaskService> Create(string contentId)
    {
        var publishTask = new ContentPublishTaskService(contentId, userSessionService, awsHttpClient);

        return ValueTask.FromResult(publishTask);
    }
}