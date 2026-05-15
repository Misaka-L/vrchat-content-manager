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

public sealed class ContentPublishTaskService
{
    /// <summary>
    /// The serializable state snapshot of the task.
    /// All task data lives here so it can be persisted and restored without
    /// touching runtime service references.
    /// </summary>
    public ContentPublishTaskState State { get; }

    #region Convenience Properties (delegating to State)

    public string TaskId => State.TaskId;

    public string ContentId => State.ContentId;
    public string ContentName => State.ContentName;
    public string ContentType => State.ContentType;
    public string ContentPlatform => State.ContentPlatform;

    public DateTimeOffset CreatedTime => State.CreatedTime;

    public string ProgressText => State.ProgressText;
    public ContentPublishTaskStatus Status => State.Status;
    public double? ProgressValue => State.ProgressValue;

    public PublishTaskStage CurrentStage => State.CurrentStage;

    #endregion

    /// <summary>
    /// The last error exception object (runtime-only, not serializable).
    /// For a serializable error description use <see cref="State"/>.<see cref="ContentPublishTaskState.ErrorMessage"/>.
    /// </summary>
    public Exception? LastError { get; private set; }

    private readonly IFileService _fileService;
    private readonly BundleProcessService _bundleProcessService;

    private readonly ILogger<ContentPublishTaskService> _logger;

    private readonly IContentPublisher _contentPublisher;

    private readonly PublishStageProgressReporter _progressReporter;

    public bool CanPublish => _contentPublisher.CanPublish();

    private readonly IPublisher<PublishTaskProgressChangedEvent> _progressPublisher;
    public event EventHandler<PublishTaskProgressEventArg>? ProgressChanged;

    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Internal constructor — called by <see cref="ContentPublishTaskFactory"/>.
    /// Accepts a fully-populated <see cref="ContentPublishTaskState"/> plus DI-injected services.
    /// Keeping all data in a single model parameter makes it easy to add/remove fields
    /// without changing this constructor signature.
    /// </summary>
    internal ContentPublishTaskService(
        ContentPublishTaskState state,
        IFileService fileService,
        ILogger<ContentPublishTaskService> logger,
        IContentPublisher contentPublisher,
        BundleProcessService bundleProcessService,
        IPublisher<PublishTaskProgressChangedEvent> progressPublisher)
    {
        State = state;

        _fileService = fileService;
        _contentPublisher = contentPublisher;
        _bundleProcessService = bundleProcessService;
        _progressPublisher = progressPublisher;
        _logger = logger;

        _progressReporter = new PublishStageProgressReporter((text, progress) => UpdateProgress(text, progress));
    }

    public void Start()
    {
        if (State.Status is
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
                   State.TaskId, State.ContentType, State.ContentName, State.ContentId, State.ContentPlatform,
                   State.RawBundleFileId)
              )
        {
            LastError = null;
            if (State.CurrentStage == PublishTaskStage.Done)
            {
                UpdateProgress("Content Published", 1, ContentPublishTaskStatus.Completed);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                if (State.CurrentStage == PublishTaskStage.BundleProcessing)
                {
                    using (_logger.BeginScope("Stage {TaskStage}", State.CurrentStage))
                    {
                        UpdateProgress("Preparing to process bundle file...", null);

                        await ProcessBundleAsync(cancellationToken);
                        State.CurrentStage = PublishTaskStage.ContentPublishing;
                    }
                }

                if (State.CurrentStage == PublishTaskStage.ContentPublishing)
                {
                    if (!_contentPublisher.CanPublish())
                        throw new InvalidOperationException("Account session expired or invalid.");

                    using (_logger.BeginScope(
                               "Stage {TaskStage} Publishing bundle file {FinalBundleFileId}",
                               State.CurrentStage,
                               State.BundleFileId)
                          )
                    {
                        UpdateProgress("Preparing for publish...", null);

                        await PublishAsync(cancellationToken);
                        State.CurrentStage = PublishTaskStage.Done;
                    }
                }

                UpdateProgress("Content Published", 1, ContentPublishTaskStatus.Completed);
                LastError = null;
            }
            catch (OperationCanceledException ex) when (_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogError(ex, "Publish task for content {ContentId} was cancelled.", State.ContentId);
                UpdateProgress("Task was cancelled.", 1, ContentPublishTaskStatus.Canceled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing content {ContentId}", State.ContentId);
                LastError = ex;
                State.ErrorMessage = ex.Message;
                UpdateProgress(ex.Message, 1, ContentPublishTaskStatus.Failed);
            }
        }
    }

    private async ValueTask ProcessBundleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing bundle file {BundleFileId} for content ({ContentId}) {ContentPlatform} {ContentName}",
            State.RawBundleFileId, State.ContentId, State.ContentPlatform, State.ContentName);

        using (StopwatchScope.Enter(watch => _logger.LogInformation(
                   "Bundle file {BundleFileId} processing for content ({ContentId}) {ContentPlatform} {ContentName} took {ElapsedMilliseconds} ms",
                   State.RawBundleFileId, State.ContentId, State.ContentPlatform, State.ContentName,
                   watch.ElapsedMilliseconds)))
        {
            var progressReporter = new PublishStageProgressReporter((message, progress) =>
            {
                _logger.LogInformation("Bundle Processing: {Message} ({Progress:P2})", message, progress);
                _progressReporter.Report(message, progress);
            });

            await using var rawBundleStream = await _fileService.GetFileAsync(State.RawBundleFileId);
            if (rawBundleStream is null)
                throw new FileNotFoundException("Raw bundle file not found.", State.RawBundleFileId);

            var outputBundleFile = await _fileService.GetUploadFileStreamAsync("processed_bundle.bundle");
            try
            {
                await using var outputBundleFileStream = outputBundleFile.FileStream;

                var processOptions = State.ContentType switch
                {
                    "world" => new WorldBundleProcessOptions(State.ContentId, []),
                    "avatar" => new AvatarBundleProcessOptions(State.ContentId, []),
                    _ => new BundleProcessOptions(State.ContentId, [])
                };

                await _bundleProcessService.ProcessBundleAsync(
                    rawBundleStream,
                    outputBundleFileStream,
                    processOptions,
                    progressReporter,
                    cancellationToken);

                State.BundleFileId = outputBundleFile.FileId;
            }
            catch
            {
                if (await _fileService.IsFileExistAsync(outputBundleFile.FileId))
                    await _fileService.DeleteFileAsync(outputBundleFile.FileId);
                throw;
            }
        }

        try
        {
            await _fileService.DeleteFileAsync(State.RawBundleFileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete raw bundle file {BundleFileId}", State.RawBundleFileId);
        }
    }

    private async ValueTask PublishAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Bundle processing for content ({ContentId}) {ContentPlatform} {ContentName} completed, preparing to publish.",
            State.ContentId, State.ContentPlatform, State.ContentName);

        using (StopwatchScope.Enter(watch => _logger.LogInformation(
                   "Publish content ({ContentId}) {ContentPlatform} {ContentName} took {ElapsedMilliseconds} ms",
                   State.ContentId, State.ContentPlatform, State.ContentName, watch.ElapsedMilliseconds)))
        {
            await _contentPublisher.PublishAsync(
                State.BundleFileId, State.ThumbnailFileId, State.Description, State.Tags, State.ReleaseStatus,
                _progressReporter, cancellationToken);
        }

        await _fileService.DeleteFileAsync(State.BundleFileId);
    }

    public async ValueTask CancelAsync()
    {
        if (State.Status != ContentPublishTaskStatus.InProgress)
            throw new InvalidOperationException("Cannot cancel a task that is not in progress.");

        UpdateProgress("Cancelling task...", null, ContentPublishTaskStatus.Cancelling);
        await _cancellationTokenSource.CancelAsync();
    }

    public async ValueTask CleanupAsync()
    {
        if (State.Status is not (ContentPublishTaskStatus.Completed
            or ContentPublishTaskStatus.Canceled
            or ContentPublishTaskStatus.Failed))
            throw new InvalidOperationException("Can only cleanup a task that is completed, canceled or failed.");

        State.Status = ContentPublishTaskStatus.Disposed;

        if (await _fileService.IsFileExistAsync(State.RawBundleFileId))
            await _fileService.DeleteFileAsync(State.RawBundleFileId);

        if (await _fileService.IsFileExistAsync(State.BundleFileId))
            await _fileService.DeleteFileAsync(State.BundleFileId);
    }

    private void UpdateProgress(string text, double? value,
        ContentPublishTaskStatus status = ContentPublishTaskStatus.InProgress)
    {
        State.ProgressText = text;
        State.ProgressValue = value;
        State.Status = status;
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
    IFileService fileService,
    ILogger<ContentPublishTaskService> logger,
    BundleProcessService bundleProcessService,
    IPublisher<PublishTaskProgressChangedEvent> progressPublisher)
{
    /// <summary>
    /// Creates a new publish task from the given state model.
    /// Validates the bundle file exists, calls <see cref="IContentPublisher.BeforePublishTaskAsync"/>,
    /// and populates <see cref="ContentPublishTaskState.ContentName"/> /
    /// <see cref="ContentPublishTaskState.ContentType"/> /
    /// <see cref="ContentPublishTaskState.ContentPlatform"/> from the publisher.
    /// </summary>
    public async ValueTask<ContentPublishTaskService> CreateAsync(
        ContentPublishTaskState state,
        IContentPublisher contentPublisher)
    {
        if (!await fileService.IsFileExistAsync(state.RawBundleFileId))
            throw new ProvideFileIdNotFoundException(state.RawBundleFileId);

        await contentPublisher.BeforePublishTaskAsync(
            state.ThumbnailFileId, state.Description, state.Tags, state.ReleaseStatus);

        // Populate content metadata from publisher
        state.ContentName = contentPublisher.GetContentName();
        state.ContentType = contentPublisher.GetContentType();
        state.ContentPlatform = contentPublisher.GetContentPlatform();
        state.BundleFileId = state.RawBundleFileId;
        state.CreatedTime = DateTimeOffset.Now;

        return new ContentPublishTaskService(
            state,
            fileService,
            logger,
            contentPublisher,
            bundleProcessService,
            progressPublisher);
    }

    /// <summary>
    /// Restores a publish task from an existing state snapshot.
    /// Does NOT re-validate file existence or call
    /// <see cref="IContentPublisher.BeforePublishTaskAsync"/>,
    /// since the task may already be at a later stage.
    /// </summary>
    public async ValueTask<ContentPublishTaskService> CreateFromStateAsync(
        ContentPublishTaskState restoredState,
        IContentPublisher contentPublisher)
    {
        return new ContentPublishTaskService(
            restoredState,
            fileService,
            logger,
            contentPublisher,
            bundleProcessService,
            progressPublisher);
    }
}