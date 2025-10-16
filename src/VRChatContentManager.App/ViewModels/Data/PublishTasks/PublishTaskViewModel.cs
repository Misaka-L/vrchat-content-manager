using Avalonia.Threading;
using VRChatContentManager.Core.Services.PublishTask;

namespace VRChatContentManager.App.ViewModels.Data.PublishTasks;

public sealed class PublishTaskViewModel : ViewModelBase
{
    public string ContentId => _publishTaskService.ContentId;
    public string ProgressText => _publishTaskService.ProgressText;
    public double? ProgressValue => _publishTaskService.ProgressValue * 100;
    public bool IsIndeterminate => !ProgressValue.HasValue;

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
            });
        };
    }
}

public sealed class PublishTaskViewModelFactory
{
    public PublishTaskViewModel Create(ContentPublishTaskService publishTaskService)
        => new(publishTaskService);
}