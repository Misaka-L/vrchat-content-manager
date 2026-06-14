using System.Diagnostics;
using AssetsTools.NET;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Telemetry;

namespace VRChatContentPublisher.BundleProcessCore.Services;

public sealed class BundleProcessService(BundleProcessPipelineOptions pipelineOptions)
{
    private readonly BundleProcessPipeline _processPipeline = new(pipelineOptions);

    public async ValueTask ProcessBundleAsync(
        Stream bundleRawStream,
        Stream outputStream,
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
            .StartActivity("ProcessBundle")
            ?.SetTag("content_id", options.ContentId);

        cancellationToken.ThrowIfCancellationRequested();

        var bundleStream = bundleRawStream;
        var isTempFileCreated = false;

        if (!bundleRawStream.CanSeek)
        {
            isTempFileCreated = true;
            bundleStream = await CopyToTempFileAsync(bundleStream, progressReporter, cancellationToken);
        }

        try
        {
            // Step.1 Bundle Preprocess (Decompress if needed)
            var result =
                await PreprocessBundleAsync(bundleStream, progressReporter, !isTempFileCreated, cancellationToken);
            if (result.IsTempFileCreated)
                isTempFileCreated = true;

            bundleStream = result.BundleStream;

            // Step.2 Run processing pipeline
            bundleStream = await _processPipeline.ProcessAsync(
                bundleStream, options, progressReporter, !isTempFileCreated, cancellationToken);

            // Step.3 Bundle Compress
            await CompressBundleAsync(bundleStream, outputStream, progressReporter, !isTempFileCreated,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            if (isTempFileCreated)
            {
                bundleStream.Close();
            }
        }
    }

    private async ValueTask<Stream> CopyToTempFileAsync(
        Stream stream,
        IProcessProgressReporter? progressReporter,
        CancellationToken cancellationToken = default
    )
    {
        using (var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                   .StartActivity("CopyRawBundleToTempFile"))
        {
            progressReporter?.Report("Copying bundle to temporary file...");

            var fileStream = CreateTempStream();
            try
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fileStream.Close();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }

            return fileStream;
        }
    }

    private async ValueTask<BundlePreprocessResult> PreprocessBundleAsync(
        Stream stream,
        IProcessProgressReporter? progressReporter,
        bool leaveOpen = true,
        CancellationToken cancellationToken = default
    )
    {
        using (var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                   .StartActivity("PreprocessBundle"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bundleFile = new AssetBundleFile();

            using var bundleReader = new AssetsFileReader(stream, true);
            bundleFile.Read(bundleReader);

            if (!bundleFile.DataIsCompressed)
            {
                bundleFile.Close();
                return new BundlePreprocessResult(stream, false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            progressReporter?.Report("Decompressing bundle file...");
            var tempBundleStream = CreateTempStream();

            try
            {
                using (BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                           .StartActivity("DecompressBundle"))
                {
                    await Task.Factory.StartNew(() =>
                        {
                            var bundleWriter = new AssetsFileWriter(tempBundleStream);
                            bundleFile.Unpack(bundleWriter, cancellationToken);
                        }, cancellationToken, TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }

                if (!leaveOpen)
                    stream.Close();

                return new BundlePreprocessResult(tempBundleStream, true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                tempBundleStream.Close();
                throw;
            }
        }
    }

    private async ValueTask CompressBundleAsync(
        Stream stream,
        Stream outputStream,
        IProcessProgressReporter? progressReporter,
        bool leaveOpen = true,
        CancellationToken cancellationToken = default
    )
    {
        using (var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                   .StartActivity("CompressBundle"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressReporter?.Report("Compressing bundle file...");

            var bundleFile = new AssetBundleFile();

            try
            {
                await Task.Factory.StartNew(() =>
                    {
                        using var bundleReader = new AssetsFileReader(stream, leaveOpen);
                        bundleFile.Read(bundleReader);

                        using var writer = new AssetsFileWriter(outputStream, true);
                        bundleFile.Pack(writer, AssetBundleCompressionType.LZMA, cancellationToken: cancellationToken);
                    }, cancellationToken, TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                bundleFile.Close();
            }
        }
    }

    private Stream CreateTempStream()
    {
        var tempFolderPath = pipelineOptions.TempFolderPath ??
                             Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-bundle-process");

        var tempFilePath = Path.Combine(
            tempFolderPath,
            Guid.NewGuid().ToString("N") + "-bundle.tmp"
        );

        return File.Create(tempFilePath, 4096,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Asynchronous);
    }
}

public sealed class BundlePreprocessResult(Stream bundleStream, bool isTempFileCreated)
{
    public Stream BundleStream { get; } = bundleStream;
    public bool IsTempFileCreated { get; } = isTempFileCreated;
}