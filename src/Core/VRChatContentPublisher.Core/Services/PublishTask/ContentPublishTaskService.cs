using Microsoft.Extensions.Logging;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;
using VRChatContentPublisher.ConnectCore.Exceptions;
using VRChatContentPublisher.ConnectCore.Services;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;
using VRChatContentPublisher.Core.Utils;

namespace VRChatContentPublisher.Core.Services.PublishTask;

public sealed class ContentPublishTaskService
{
    public string TaskId { get; }

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

    #endregion

    #region Progress

    public event EventHandler<PublishTaskProgressEventArg>? ProgressChanged;

    public string ProgressText { get; private set; } = "Waiting for task started...";
    public ContentPublishTaskStatus Status { get; private set; } = ContentPublishTaskStatus.Pending;
    public double? ProgressValue { get; private set; }
    public Exception? LastError { get; private set; }

    #endregion

    #region Task Inner State

    public PublishTaskStage CurrentStage { get; private set; } = PublishTaskStage.BundleProcessing;
    private readonly string _rawBundleFileId;
    private readonly string? _thumbnailFileId;
    private readonly string? _description;
    private readonly string[]? _tags;
    private readonly string? _releaseStatus;

    private string _bundleFileId;

    private CancellationTokenSource _cancellationTokenSource = new();

    #endregion

    internal ContentPublishTaskService(
        string taskId,
        string contentId, string rawBundleFileId,
        string? thumbnailFileId, string? description, string[]? tags, string? releaseStatus,
        HttpClient awsHttpClient, IFileService tempFileService, ILogger<ContentPublishTaskService> logger,
        IContentPublisher contentPublisher, BundleProcessService bundleProcessService)
    {
        TaskId = taskId;

        ContentId = contentId;
        ContentName = contentPublisher.GetContentName();
        ContentType = contentPublisher.GetContentType();
        ContentPlatform = contentPublisher.GetContentPlatform();

        _rawBundleFileId = rawBundleFileId;
        _thumbnailFileId = thumbnailFileId;
        _description = description;
        _tags = tags;
        _releaseStatus = releaseStatus;
        _bundleFileId = rawBundleFileId;

        _awsHttpClient = awsHttpClient;
        _tempFileService = tempFileService;
        _contentPublisher = contentPublisher;
        _bundleProcessService = bundleProcessService;
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
                   TaskId, ContentType, ContentName, ContentId, ContentPlatform, _rawBundleFileId)
              )
        {
            LastError = null;
            if (CurrentStage == PublishTaskStage.Done)
            {
                UpdateProgress("Content Published", 1, ContentPublishTaskStatus.Completed);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                if (CurrentStage == PublishTaskStage.BundleProcessing)
                {
                    using (_logger.BeginScope("Stage {TaskStage}", CurrentStage))
                    {
                        UpdateProgress("Preparing to process bundle file...", null);

                        await ProcessBundleAsync(cancellationToken);
                        CurrentStage = PublishTaskStage.ContentPublishing;
                    }
                }

                if (CurrentStage == PublishTaskStage.ContentPublishing)
                {
                    using (_logger.BeginScope(
                               "Stage {TaskStage} Publishing bundle file {FinalBundleFileId}",
                               CurrentStage,
                               _bundleFileId)
                          )
                    {
                        UpdateProgress("Preparing for publish...", null);

                        await PublishAsync(cancellationToken);
                        CurrentStage = PublishTaskStage.Done;
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
            _rawBundleFileId, ContentId, ContentPlatform, ContentName);

        using (StopwatchScope.Enter(watch => _logger.LogInformation(
                   "Bundle file {BundleFileId} processing for content ({ContentId}) {ContentPlatform} {ContentName} took {ElapsedMilliseconds} ms",
                   _rawBundleFileId, ContentId, ContentPlatform, ContentName, watch.ElapsedMilliseconds)))
        {
            var progressReporter = new PublishStageProgressReporter((message, progress) =>
            {
                _logger.LogInformation("Bundle Processing: {Message} ({Progress:P2})", message, progress);
                _progressReporter.Report(message, progress);
            });

            await using var rawBundleStream = await _tempFileService.GetFileAsync(_rawBundleFileId);
            if (rawBundleStream is null)
                throw new FileNotFoundException("Raw bundle file not found.", _rawBundleFileId);

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

                _bundleFileId = outputBundleFile.FileId;
            }
            catch
            {
                if (await _tempFileService.IsFileExistAsync(outputBundleFile.FileId))
                    await  _tempFileService.DeleteFileAsync(outputBundleFile.FileId);
                throw;
            }
        }

        try
        {
            await _tempFileService.DeleteFileAsync(_rawBundleFileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete raw bundle file {BundleFileId}", _rawBundleFileId);
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
                _bundleFileId, _thumbnailFileId, _description, _tags, _releaseStatus
                , _awsHttpClient, _progressReporter, cancellationToken);
        }

        await _tempFileService.DeleteFileAsync(_bundleFileId);
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
        if (Status is not (ContentPublishTaskStatus.Canceled or ContentPublishTaskStatus.Failed))
            throw new InvalidOperationException("Can only cleanup a task that is canceled or failed.");

        Status = ContentPublishTaskStatus.Disposed;

        if (await _tempFileService.IsFileExistAsync(_rawBundleFileId))
            await _tempFileService.DeleteFileAsync(_rawBundleFileId);

        if (await _tempFileService.IsFileExistAsync(_bundleFileId))
            await _tempFileService.DeleteFileAsync(_bundleFileId);
    }

    private void UpdateProgress(string text, double? value,
        ContentPublishTaskStatus status = ContentPublishTaskStatus.InProgress)
    {
        ProgressText = text;
        ProgressValue = value;
        Status = status;
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
    BundleProcessService bundleProcessService)
{
    public async ValueTask<ContentPublishTaskService> Create(
        string taskId,
        string contentId,
        string bundleFileId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus,
        IContentPublisher contentPublisher)
    {
        if (!await tempFileService.IsFileExistAsync(bundleFileId))
            throw new ProvideFileIdNotFoundException(bundleFileId);

        var publishTask = new ContentPublishTaskService(
            taskId,
            contentId,
            bundleFileId,
            thumbnailFileId,
            description,
            tags,
            releaseStatus,
            awsHttpClient,
            tempFileService,
            logger,
            contentPublisher,
            bundleProcessService
        );

        return publishTask;
    }
}