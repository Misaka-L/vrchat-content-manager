using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Services.PublishTask;

namespace VRChatContentPublisher.App.ViewModels;

public sealed class TaskErrorReportWindowViewModel(ContentPublishTaskService publishTaskService) : ViewModelBase
{
    public string ContentName => publishTaskService.ContentName;
    public string ContentType => publishTaskService.ContentType;
    public string ContentPlatform => publishTaskService.ContentPlatform;

    public string ExceptionText => publishTaskService.LastError?.ToString() ?? "No error information available.";
    public string PublishStage => publishTaskService.CurrentStage.ToString();

    public string LogFolderPath => AppStorageService.GetLogsPath();
}