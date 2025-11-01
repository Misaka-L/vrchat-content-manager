using Avalonia.Threading;
using VRChatContentManager.Core.Models;
using VRChatContentManager.Core.Services.PublishTask;

namespace VRChatContentManager.App.ViewModels.Data.PublishTasks;

public sealed class PublishTaskViewModel : ViewModelBase
{
    public string ContentId => _publishTaskService.ContentId;
    public string ContentName => _publishTaskService.ContentName;
    public string ContentType => _publishTaskService.ContentType;
    public string ContentPlatform => _publishTaskService.ContentPlatform;
    
    public string ProgressText => _publishTaskService.ProgressText;
    public double? ProgressValue => _publishTaskService.ProgressValue * 100;
    public bool IsIndeterminate => !ProgressValue.HasValue;
    
    public ContentPublishTaskStatus Status => _publishTaskService.Status;

    private readonly ContentPublishTaskService _publishTaskService;

    public PublishTaskViewModel(ContentPublishTaskService publishTaskService)
    {
        _publishTaskService = publishTaskService;

        _publishTaskService.ProgressChanged += (_, _) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressValue));
                OnPropertyChanged(nameof(IsIndeterminate));
                OnPropertyChanged(nameof(Status));
            });
        };
    }
}

public sealed class PublishTaskViewModelFactory
{
    public PublishTaskViewModel Create(ContentPublishTaskService publishTaskService)
        => new(publishTaskService);
}