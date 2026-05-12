using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Files;
using VRChatContentPublisher.Core.Resilience;

namespace VRChatContentPublisher.Core.Services.VRChatApi.S3;

public sealed class ConcurrentMultipartUploader(
    VRChatApiClient apiClient,
    HttpClient awsClient,
    ILogger<ConcurrentMultipartUploader> logger,
    Stream fileStream,
    string fileId,
    int fileVersion,
    VRChatApiFileType fileType,
    CancellationToken cancellationToken)
{
    public event EventHandler<double>? ProgressChanged;

    private const long ChunkSize = 50 * 1024 * 1024;
    private const int MaxConcurrentUploads = 3;
    private readonly ConcurrentDictionary<int, string> _eTags = new();

    // Progress tracking
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(100);
    private long _completedChunkBytes;
    private readonly ConcurrentDictionary<int, long> _chunkProgress = new();

    // Speed tracking
    private const int SpeedWindowSeconds = 3;
    private readonly ConcurrentQueue<SpeedSample> _speedSamples = new();
    private long _speedSampleTotalBytes;

    private long _currentSpeedBytesPerSecond;
    public long CurrentSpeedBytesPerSecond => Interlocked.Read(ref _currentSpeedBytesPerSecond);

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
            logger.LogError(ex, "An error occurred during the upload of file {FileId} version {FileVersion}", fileId,
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

    private async Task UploadChunkAsync(int partNumber, byte[] buffer, int bytesRead, CancellationToken ct)
    {
        logger.LogInformation(
            "Creating upload chunk {PartNumber} (Size: {Size:F2} MiB) for file {FileId}",
            partNumber, (double)bytesRead / 1024 / 1024, fileId);

        // 1. Get the pre-signed URL for this part from the VRChat API (only once — URLs are reusable).
        var uploadUrl = await apiClient.GetFilePartUploadUrlAsync(fileId, fileVersion, partNumber, fileType, ct);

        // 2. Upload the data to S3 with retry. On each retry attempt we reset in-flight progress
        //    for this chunk so the progress bar never double-counts bytes from a failed attempt.
        var retryPipeline = CreateRetryPipeline(partNumber);
        await retryPipeline.ExecuteAsync(async innerCt =>
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
        }, ct);

        // Mark chunk as fully completed — move bytes from in-flight to completed.
        Interlocked.Add(ref _completedChunkBytes, bytesRead);
        _chunkProgress.TryRemove(partNumber, out _);

        logger.LogInformation("Completed upload for chunk {PartNumber} for file {FileId}", partNumber, fileId);
    }

    #endregion

    #region Progress Tracking

    private void OnChunkProgress(int partNumber, long bytes)
    {
        _chunkProgress.AddOrUpdate(partNumber, bytes, (_, prev) => prev + bytes);
        RecordSpeedSample(bytes);
    }

    private void FireProgressChanged()
    {
        if (fileStream.Length == 0) return;

        var inFlight = _chunkProgress.Values.Sum();
        var total = _completedChunkBytes + inFlight;
        var progress = Math.Min((double)total / fileStream.Length, 1.0);
        ProgressChanged?.Invoke(this, progress);
    }

    private void RecordSpeedSample(long bytes)
    {
        var now = Stopwatch.GetTimestamp();
        _speedSamples.Enqueue(new SpeedSample(bytes, now));
        Interlocked.Add(ref _speedSampleTotalBytes, bytes);

        // Prune samples older than the sliding window.
        var cutoff = now - SpeedWindowSeconds * Stopwatch.Frequency;
        while (_speedSamples.TryPeek(out var sample) && sample.Timestamp < cutoff)
        {
            if (_speedSamples.TryDequeue(out var removed))
                Interlocked.Add(ref _speedSampleTotalBytes, -removed.Bytes);
        }

        // Calculate current speed from the sliding window.
        if (_speedSamples.TryPeek(out var oldest) && oldest.Timestamp < now)
        {
            var elapsedSeconds = (double)(now - oldest.Timestamp) / Stopwatch.Frequency;
            if (elapsedSeconds > 0.1)
            {
                Interlocked.Exchange(ref _currentSpeedBytesPerSecond,
                    (long)(Interlocked.Read(ref _speedSampleTotalBytes) / elapsedSeconds));
            }
        }
    }

    private readonly record struct SpeedSample(long Bytes, long Timestamp);

    #endregion
}

public sealed class ConcurrentMultipartUploaderFactory(ILogger<ConcurrentMultipartUploader> logger)
{
    public ConcurrentMultipartUploader Create(Stream fileStream, string fileId, int fileVersion,
        VRChatApiFileType fileType, VRChatApiClient apiClient, HttpClient awsClient,
        CancellationToken cancellationToken = default)
    {
        return new ConcurrentMultipartUploader(apiClient, awsClient, logger, fileStream, fileId, fileVersion, fileType,
            cancellationToken);
    }
}