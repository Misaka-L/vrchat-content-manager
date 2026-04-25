using MessagePipe;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;
using VRChatContentPublisher.ConnectCore.Exceptions;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.Events.PublishTask;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core.Services.PublishTask;

public sealed class ContentPublishTaskCreateOptions(
    string contentId,
    string rawBundleFileId,
    string? thumbnailFileId,
    string? description,
    string[]? tags,
    string? releaseStatus)
{
    public string ContentId => contentId;
    public string RawBundleFileId => rawBundleFileId;
    public string? ThumbnailFileId => thumbnailFileId;
    public string? Description => description;
    public string[]? Tags => tags;
    public string? ReleaseStatus => releaseStatus;
}

public sealed class ContentPublishTaskStageInformation(
    PublishTaskStage currentStage,
    string bundleFileId,
    string taskId)
{
    public PublishTaskStage CurrentStage { get; set; } = currentStage;
    public string BundleFileId { get; set; } = bundleFileId;
    public string TaskId { get; set; } = taskId;
}

public sealed class ContentPublishTaskService
{
    public string TaskId { get; }
    public PublishTaskStage CurrentStage => _stageInformation.CurrentStage;

    private readonly HttpClient _awsHttpClient;
    private readonly IFileService _tempFileService;
    private readonly BundleProcessService _bundleProcessService;

    private readonly ILogger<ContentPublishTaskService> _logger;

    private readonly IContentPublisher _contentPublisher;

    private readonly PublishStageProgressReporter _progressReporter;

    #region Content Information

    public string ContentId { get; }
    public string ContentName { get; }
    public string ContentType { get; }
    public string ContentPlatform { get; }

    public bool CanPublish => _contentPublisher.CanPublish();

    #endregion

    #region Progress

    private readonly IPublisher<PublishTaskProgressChangedEvent> _progressPublisher;
    public event EventHandler<PublishTaskProgressEventArg>? ProgressChanged;

    public DateTimeOffset CreatedTime { get; } = DateTimeOffset.Now;
    public string ProgressText { get; private set; } = "Waiting for task started...";
    public ContentPublishTaskStatus Status { get; private set; } = ContentPublishTaskStatus.Pending;
    public double? ProgressValue { get; private set; }
    public Exception? LastError { get; private set; }

    #endregion

    #region Task Inner State

    private readonly ContentPublishTaskCreateOptions _createOptions;
    private readonly ContentPublishTaskStageInformation _stageInformation;

    private CancellationTokenSource _cancellationTokenSource = new();

    #endregion

    internal ContentPublishTaskService(
        // Factory Create Options
        ContentPublishTaskCreateOptions createOptions,
        IContentPublisher contentPublisher,
        ContentPublishTaskStageInformation? stageInformation,
        // DI
        HttpClient awsHttpClient, IFileService tempFileService, ILogger<ContentPublishTaskService> logger,
        BundleProcessService bundleProcessService, IPublisher<PublishTaskProgressChangedEvent> progressPublisher)
    {
        _createOptions = createOptions;

        _stageInformation = stageInformation ?? new ContentPublishTaskStageInformation(
            PublishTaskStage.BundleProcessing,
            createOptions.RawBundleFileId,
            Guid.CreateVersion7().ToString()
        );

        TaskId = _stageInformation.TaskId;

        ContentId = createOptions.ContentId;
        ContentName = contentPublisher.GetContentName();
        ContentType = contentPublisher.GetContentType();
        ContentPlatform = contentPublisher.GetContentPlatform();

        _awsHttpClient = awsHttpClient;
        _tempFileService = tempFileService;
        _contentPublisher = contentPublisher;
        _bundleProcessService = bundleProcessService;
        _progressPublisher = progressPublisher;
        _logger = logger;

        _progressReporter = new PublishStageProgressReporter((text, progress) => UpdateProgress(text, progress));
    }

    public void Start()
    {
        if (Status is
            ContentPublishTaskStatus.Completed or
            ContentPublishTaskStatus.Cancelling or
            ContentPublishTaskStatus.InProgress)
            throw new InvalidOperationException(
                "Cannot start a task that in completed, cancelling or in progress state.");

        _ = Task.Factory.StartNew(StartTaskCoreAsync, TaskCreationOptions.LongRunning);
    }

    private async Task StartTaskCoreAsync()
    {
        using (_logger.BeginScope(
                   "Publish task ({TaskId}) for {ContentType} {ContentName} ({ContentId}) on platform {ContentPlatform}, Raw BundleFileId: {RawBundleFileId}",
                   TaskId, ContentType, ContentName, ContentId, ContentPlatform,
                   _createOptions.RawBundleFileId)
              )
        {
            LastError = null;
            if (_stageInformation.CurrentStage == PublishTaskStage.Done)
            {
                UpdateProgress("Content Published", 1, ContentPublishTaskStatus.Completed);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                if (_stageInformation.CurrentStage == PublishTaskStage.BundleProcessing)
                {
                    using (_logger.BeginScope("Stage {TaskStage}", _stageInformation.CurrentStage))
                    {
                        UpdateProgress("Preparing to process bundle file...", null);

                        await ProcessBundleAsync(cancellationToken);
                        _stageInformation.CurrentStage = PublishTaskStage.ContentPublishing;
                    }
                }

                if (_stageInformation.CurrentStage == PublishTaskStage.ContentPublishing)
                {
                    if (!_contentPublisher.CanPublish())
                        throw new InvalidOperationException("Account session expired or invalid.");

                    using (_logger.BeginScope(
                               "Stage {TaskStage} Publishing bundle file {FinalBundleFileId}",
                               _stageInformation.CurrentStage, _stageInformation.BundleFileId)
                          )
                    {
                        UpdateProgress("Preparing for publish...", null);

                        await PublishAsync(cancellationToken);
                        _stageInformation.CurrentStage = PublishTaskStage.Done;
                    }
                }

                UpdateProgress("Content Published", 1, ContentPublishTaskStatus.Completed);
                LastError = null;
            }
            catch (OperationCanceledException ex) when (_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogError(ex, "Publish task for content {ContentId} was cancelled.", ContentId);
                UpdateProgress("Task was cancelled.", 1, ContentPublishTaskStatus.Canceled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing content {ContentId}", ContentId);
                LastError = ex;
                UpdateProgress(ex.Message, 1, ContentPublishTaskStatus.Failed);
            }
        }
    }

    private async ValueTask ProcessBundleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing bundle file {BundleFileId} for content ({ContentId}) {ContentPlatform} {ContentName}",
            _createOptions.RawBundleFileId, ContentId, ContentPlatform, ContentName);

        using (StopwatchScope.Enter(watch => _logger.LogInformation(
                   "Bundle file {BundleFileId} processing for content ({ContentId}) {ContentPlatform} {ContentName} took {ElapsedMilliseconds} ms",
                   _createOptions.RawBundleFileId, ContentId, ContentPlatform, ContentName,
                   watch.ElapsedMilliseconds)))
        {
            var progressReporter = new PublishStageProgressReporter((message, progress) =>
            {
                _logger.LogInformation("Bundle Processing: {Message} ({Progress:P2})", message, progress);
                _progressReporter.Report(message, progress);
            });

            await using var rawBundleStream =
                await _tempFileService.GetFileAsync(_createOptions.RawBundleFileId);
            if (rawBundleStream is null)
                throw new FileNotFoundException("Raw bundle file not found.",
                    _createOptions.RawBundleFileId);

            var outputBundleFile = await _tempFileService.GetUploadFileStreamAsync("processed_bundle.bundle");
            try
            {
                await using var outputBundleFileStream = outputBundleFile.FileStream;

                var processOptions = ContentType switch
                {
                    "world" => new WorldBundleProcessOptions(ContentId, []),
                    "avatar" => new AvatarBundleProcessOptions(ContentId, []),
                    _ => new BundleProcessOptions(ContentId, [])
                };

                await _bundleProcessService.ProcessBundleAsync(
                    rawBundleStream,
                    outputBundleFileStream,
                    processOptions,
                    progressReporter,
                    cancellationToken);

                _stageInformation.BundleFileId = outputBundleFile.FileId;
            }
            catch
            {
                if (await _tempFileService.IsFileExistAsync(outputBundleFile.FileId))
                    await _tempFileService.DeleteFileAsync(outputBundleFile.FileId);
                throw;
            }
        }

        try
        {
            await _tempFileService.DeleteFileAsync(_createOptions.RawBundleFileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete raw bundle file {BundleFileId}",
                _createOptions.RawBundleFileId);
        }
    }

    private async ValueTask PublishAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Bundle processing for content ({ContentId}) {ContentPlatform} {ContentName} completed, preparing to publish.",
            ContentId, ContentPlatform, ContentName);

        using (StopwatchScope.Enter(watch => _logger.LogInformation(
                   "Publish content ({ContentId}) {ContentPlatform} {ContentName} took {ElapsedMilliseconds} ms",
                   ContentId, ContentPlatform, ContentName, watch.ElapsedMilliseconds)))
        {
            await _contentPublisher.PublishAsync(
                _stageInformation.BundleFileId,
                _createOptions.ThumbnailFileId,
                _createOptions.Description,
                _createOptions.Tags,
                _createOptions.ReleaseStatus
                , _awsHttpClient, _progressReporter, cancellationToken
            );
        }

        await _tempFileService.DeleteFileAsync(_stageInformation.BundleFileId);
    }

    public async ValueTask CancelAsync()
    {
        if (Status != ContentPublishTaskStatus.InProgress)
            throw new InvalidOperationException("Cannot cancel a task that is not in progress.");

        UpdateProgress("Cancelling task...", null, ContentPublishTaskStatus.Cancelling);
        await _cancellationTokenSource.CancelAsync();
    }

    public async ValueTask CleanupAsync()
    {
        if (Status is not (ContentPublishTaskStatus.Completed
            or ContentPublishTaskStatus.Canceled
            or ContentPublishTaskStatus.Failed))
            throw new InvalidOperationException("Can only cleanup a task that is completed, canceled or failed.");

        Status = ContentPublishTaskStatus.Disposed;

        if (await _tempFileService.IsFileExistAsync(_createOptions.RawBundleFileId))
            await _tempFileService.DeleteFileAsync(_createOptions.RawBundleFileId);

        if (await _tempFileService.IsFileExistAsync(_stageInformation.BundleFileId))
            await _tempFileService.DeleteFileAsync(_stageInformation.BundleFileId);
    }

    private void UpdateProgress(string text, double? value,
        ContentPublishTaskStatus status = ContentPublishTaskStatus.InProgress)
    {
        ProgressText = text;
        ProgressValue = value;
        Status = status;
        _progressPublisher.Publish(new PublishTaskProgressChangedEvent(this, text, value, status));
        ProgressChanged?.Invoke(this, new PublishTaskProgressEventArg(text, value, status));
    }
}

public enum PublishTaskStage
{
    BundleProcessing,
    ContentPublishing,
    Done
}

public sealed class ContentPublishTaskFactory(
    HttpClient awsHttpClient,
    IFileService tempFileService,
    ILogger<ContentPublishTaskService> logger,
    BundleProcessService bundleProcessService,
    IPublisher<PublishTaskProgressChangedEvent> progressPublisher)
{
    public async ValueTask<ContentPublishTaskService> CreateAsync(
        ContentPublishTaskCreateOptions createOptions,
        IContentPublisher contentPublisher,
        ContentPublishTaskStageInformation? stageInformation = null)
    {
        var bundleFileId = createOptions.RawBundleFileId;
        if (!await tempFileService.IsFileExistAsync(bundleFileId))
            throw new ProvideFileIdNotFoundException(bundleFileId);

        await contentPublisher.BeforePublishTaskAsync(
            createOptions.ThumbnailFileId,
            createOptions.Description,
            createOptions.Tags,
            createOptions.ReleaseStatus,
            awsHttpClient
        );

        var publishTask = new ContentPublishTaskService(
            createOptions,
            contentPublisher,
            stageInformation,
            awsHttpClient,
            tempFileService,
            logger,
            bundleProcessService,
            progressPublisher
        );

        return publishTask;
    }
}