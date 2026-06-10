using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly;
using VRChatContentPublisher.Core.Resilience;
using VRChatContentPublisher.VRChatApi.ApiClient;
using VRChatContentPublisher.VRChatApi.Models.Rest.Files;
using VRChatContentPublisher.VRChatApi.Telemetry;

namespace VRChatContentPublisher.VRChatApi;

public sealed class ConcurrentMultipartUploader(
    VRChatApiClient apiClient,
    HttpClient awsClient,
    ILogger<ConcurrentMultipartUploader> logger,
    Stream fileStream,
    string fileId,
    int fileVersion,
    VRChatApiFileType fileType,
    CancellationToken cancellationToken) : IDisposable
{
    public event EventHandler<(double ProgressPrcentage, long CurrentSpeedBytesPerSecond)>? ProgressChanged;

    private const long ChunkSize = 50 * 1024 * 1024;
    private const int MaxConcurrentUploads = 3;
    private readonly ConcurrentDictionary<int, string> _eTags = new();

    // Progress tracking
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(100);
    private long _completedChunkBytes;
    private readonly ConcurrentDictionary<int, long> _chunkProgress = new();

    // Speed tracking
    private readonly UploadSpeedTracker _speedTracker = new();

    public long CurrentSpeedBytesPerSecond => _speedTracker.GetCurrentSpeed();

    private ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline(int partNumber)
    {
        const int maxRetryAttempts = 3;
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new AppHttpRetryStrategyOptions
            {
                UseJitter = true,
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "S3 upload retry {Attempt}/{MaxAttempts} for chunk {PartNumber}",
                        args.AttemptNumber, maxRetryAttempts, partNumber);
                    return default;
                }
            })
            .Build();
    }

    #region Upload Logic

    public async Task<string[]> UploadAsync()
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (VRChatApiCoreTelemetry.VRChatApi.StartActivity("ConcurrentMultipartUpload")
                   ?.AddTag("fileId", fileId)
                   .AddTag("fileVersion", fileVersion)
                   .AddTag("fileSizeBytes", fileStream.Length)
                   .AddTag("chunkSizeBytes", ChunkSize)
                   .AddTag("maxConcurrency", MaxConcurrentUploads))
        using (logger.BeginScope("FileId: {FileId}, FileVersion: {FileVersion}", fileId, fileVersion))
        {
            logger.LogInformation(
                "Starting concurrent upload for file {FileId} version {FileVersion} with chunk size {ChunkSize}MB and concurrency {MaxConcurrency}",
                fileId, fileVersion, ChunkSize / 1024 / 1024, MaxConcurrentUploads);

            var progressTimer = new PeriodicTimer(ProgressReportInterval);
            var progressReportTask = Task.Run(async () =>
            {
                // ReSharper disable once AccessToDisposedClosure
                while (await progressTimer.WaitForNextTickAsync(CancellationToken.None))
                    FireProgressChanged();
            }, CancellationToken.None);

            using var concurrencySemaphore = new SemaphoreSlim(MaxConcurrentUploads);
            var uploadTasks = new List<Task>();
            var partNumber = 0;

            try
            {
                while (fileStream.Position < fileStream.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wait for a free slot to begin the next upload.
                    await concurrencySemaphore.WaitAsync(cancellationToken);

                    partNumber++;
                    var currentPartNumber = partNumber;

                    var bufferSize = (int)Math.Min(ChunkSize, fileStream.Length - fileStream.Position);
                    var buffer = new byte[bufferSize];
                    var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);

                    // Start the upload task for the current chunk.
                    var uploadTask = Task.Run(async () =>
                    {
                        try
                        {
                            await UploadChunkAsync(currentPartNumber, buffer, bytesRead, cancellationToken);
                        }
                        finally
                        {
                            // Release the semaphore slot once the upload is complete or has failed.
                            // ReSharper disable once AccessToDisposedClosure
                            concurrencySemaphore.Release();
                        }
                    }, cancellationToken);

                    uploadTasks.Add(uploadTask);
                }

                // Wait for all initiated upload tasks to complete.
                await Task.WhenAll(uploadTasks);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Upload for file {FileId} version {FileVersion} was canceled", fileId,
                    fileVersion);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the upload of file {FileId} version {FileVersion}",
                    fileId,
                    fileVersion);
                // Rethrow to allow the caller to handle the failure.
                throw;
            }
            finally
            {
                progressTimer.Dispose();
            }

            logger.LogInformation("Successfully uploaded all parts for file {FileId} version {FileVersion}", fileId,
                fileVersion);

            // Return the ETag for each part, ordered by part number.
            var result = _eTags.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();

            await progressReportTask;
            return result;
        }
    }

    private async Task UploadChunkAsync(int partNumber, byte[] buffer, int bytesRead, CancellationToken ct)
    {
        using var activity = VRChatApiCoreTelemetry.VRChatApi.StartActivity("UploadChunk")
            ?.AddTag("fileId", fileId)
            .AddTag("fileVersion", fileVersion)
            .AddTag("partNumber", partNumber)
            .AddTag("chunkSizeBytes", bytesRead);

        using (logger.BeginScope(
                   "FileId: {FileId}, FileVersion: {FileVersion}, PartNumber: {PartNumber}",
                   fileId, fileVersion, partNumber))
        {
            try
            {
                logger.LogInformation(
                    "Creating upload chunk {PartNumber} (Size: {Size:F2} MiB) for file {FileId}",
                    partNumber, (double)bytesRead / 1024 / 1024, fileId);

                // 1. Get the pre-signed URL for this part from the VRChat API (only once — URLs are reusable).
                var uploadUrl =
                    await apiClient.GetFilePartUploadUrlAsync(fileId, fileVersion, partNumber, fileType, ct);

                // 2. Upload the data to S3 with retry. On each retry attempt we reset in-flight progress
                //    for this chunk so the progress bar never double-counts bytes from a failed attempt.
                var retryPipeline = CreateRetryPipeline(partNumber);
                await retryPipeline.ExecuteAsync(async innerCt =>
                {
                    using var attemptActivity = VRChatApiCoreTelemetry.VRChatApi.StartActivity("UploadChunkAttempt");

                    try
                    {
                        // Reset in-flight progress for the current retry attempt.
                        _chunkProgress[partNumber] = 0;

                        using var stream = new MemoryStream(buffer, 0, bytesRead);
                        using var progressStream = new ProgressStreamContent(stream,
                            bytes => OnChunkProgress(partNumber, bytes));

                        var response = await awsClient.PutAsync(uploadUrl, progressStream, innerCt);
                        response.EnsureSuccessStatusCode();

                        // 3. Extract the ETag from the response headers. Required by S3 to complete the multipart upload.
                        var eTag = response.Headers.ETag?.Tag.Trim('\"', '\'');
                        if (string.IsNullOrEmpty(eTag))
                        {
                            throw new InvalidOperationException(
                                $"S3 did not return an ETag for part {partNumber} of file {fileId}.");
                        }

                        _eTags[partNumber] = eTag;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        attemptActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        throw;
                    }
                }, ct);

                // Mark chunk as fully completed — move bytes from in-flight to completed.
                Interlocked.Add(ref _completedChunkBytes, bytesRead);
                _chunkProgress.TryRemove(partNumber, out _);

                logger.LogInformation("Completed upload for chunk {PartNumber} for file {FileId}", partNumber, fileId);
            }
            catch (Exception e)
            {
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                throw;
            }
        }
    }

    #endregion

    #region Progress Tracking

    private void OnChunkProgress(int partNumber, long bytes)
    {
        _chunkProgress.AddOrUpdate(partNumber, bytes, (_, prev) => prev + bytes);
        _speedTracker.RecordBytes(bytes);
    }

    private void FireProgressChanged()
    {
        if (fileStream.Length == 0) return;

        var inFlight = _chunkProgress.Values.Sum();
        var total = _completedChunkBytes + inFlight;
        var progress = Math.Min((double)total / fileStream.Length, 1.0);
        ProgressChanged?.Invoke(this, (progress, _speedTracker.GetCurrentSpeed()));
    }

    #endregion

    public void Dispose()
    {
        awsClient.Dispose();
    }
}

public sealed class ConcurrentMultipartUploaderFactory(
    ILogger<ConcurrentMultipartUploader> logger
)
{
    public ConcurrentMultipartUploader Create(
        Stream fileStream,
        string fileId,
        int fileVersion,
        VRChatApiFileType fileType,
        VRChatApiClient apiClient,
        HttpClient uploadClient,
        CancellationToken cancellationToken = default)
    {
        return new ConcurrentMultipartUploader(
            apiClient,
            uploadClient,
            logger,
            fileStream,
            fileId,
            fileVersion,
            fileType,
            cancellationToken);
    }
}