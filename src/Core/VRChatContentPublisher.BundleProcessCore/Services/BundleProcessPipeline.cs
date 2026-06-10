using System.Collections.ObjectModel;
using System.Diagnostics;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Processers;
using VRChatContentPublisher.BundleProcessCore.Telemetry;
using VRChatContentPublisher.BundleProcessCore.Utils;

namespace VRChatContentPublisher.BundleProcessCore.Services;

internal sealed class BundleProcessPipeline(
    BundleProcessPipelineOptions pipelineOptions
)
{
    public async ValueTask<Stream> ProcessAsync(
        Stream bundleStream,
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter = null,
        bool leaveOpen = true,
        CancellationToken cancellationToken = default)
    {
        using var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
            .StartActivity("ProcessBundlePipeline")
            ?.SetTag("content_id", options.ContentId);

        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Factory.StartNew(() =>
                ProcessCore(bundleStream, options, progressReporter, leaveOpen, cancellationToken),
            cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    private Stream ProcessCore(
        Stream bundleStream,
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter = null,
        bool leaveOpen = true,
        CancellationToken cancellationToken = default)
    {
        progressReporter?.Report("Starting bundle processing pipeline...");

        // Load Assets Manager and Class Package
        var manager = new AssetsManager();
        try
        {
            var bundle = LoadAssetBundle(bundleStream, manager);

            cancellationToken.ThrowIfCancellationRequested();

            // Load all assets files from bundle blocks
            progressReporter?.Report("Loading assets files from bundle...");
            var (blockAssetMap, assetsFiles) =
                GetAssetFilesFromBundle(bundle, manager, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            manager.LoadClassDatabaseFromPackage(blockAssetMap.First().Value.file.Metadata.UnityVersion);

            // Determine which processors should run or should bundle processing be skipped
            progressReporter?.Report("Determining applicable processors...");
            var processer = DetermineApplicableProcessors(options, progressReporter, manager, bundle, assetsFiles);

            if (processer.Length == 0)
                return bundleStream;

            // Run each processor
            foreach (var bundleProcesser in processer)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processorName = bundleProcesser.GetType().Name;
                using (BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                           .StartActivity(processorName + ".Process")?
                           .SetTag("processor", processorName))
                {
                    progressReporter?.Report("Running " + bundleProcesser.GetType().Name);
                    bundleProcesser.Process(manager, bundle, assetsFiles, options, progressReporter);
                }
            }

            // Write back modified assets file to bundle
            progressReporter?.Report("Writing processed assets back to bundle...");
            using (BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                       .StartActivity("WriteBackModifiedAssets"))
            {
                foreach (var blockAssetPair in blockAssetMap)
                {
                    blockAssetPair.Key.SetNewData(blockAssetPair.Value.file);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                       .StartActivity("WriteBackBundle"))
            {
                var newBundleStream = CreateTempStream();
                using var writer = new AssetsFileWriter(newBundleStream, true);
                bundle.file.Write(writer);

                if (!leaveOpen)
                    bundleStream.Close();

                newBundleStream.Position = 0;
                return newBundleStream;
            }
        }
        finally
        {
            manager.UnloadAll();
        }
    }

    private static IBundleProcesser[] DetermineApplicableProcessors(
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter,
        AssetsManager manager,
        BundleFileInstance bundle,
        AssetsFileInstance[] assetsFiles)
    {
        using var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource.StartActivity();
        try
        {
            var processer = options.Processers
                .Where(p =>
                {
                    var processorName = p.GetType().Name;
                    using var shouldProcessActivity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
                        .StartActivity(processorName + ".ShouldProcess")?
                        .SetTag("processor", processorName);

                    try
                    {
                        return p.ShouldProcess(manager, bundle, assetsFiles, options, progressReporter);
                    }
                    catch (Exception e)
                    {
                        shouldProcessActivity?.SetStatus(ActivityStatusCode.Error, e.Message);
                        throw;
                    }
                })
                .ToArray();

            return processer;
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            throw;
        }
    }

    private static (
        ReadOnlyDictionary<AssetBundleDirectoryInfo, AssetsFileInstance> blockAssetMap,
        AssetsFileInstance[] assetsFiles
        ) GetAssetFilesFromBundle(
            BundleFileInstance bundle,
            AssetsManager manager,
            CancellationToken cancellationToken)
    {
        using var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource.StartActivity();

        try
        {
            var blockAssetMap = bundle.file.BlockAndDirInfo.DirectoryInfos
                .Where(info => (info.Flags & 0x04) != 0)
                .Where(info =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var blockStream = new SegmentStream(bundle.DataStream, info.Offset, info.DecompressedSize);
                    using var reader = new AssetsFileReader(blockStream);
                    return FileTypeDetector.DetectFileType(reader, 0) == DetectedFileType.AssetsFile;
                })
                .ToDictionary(info => info, info => manager.LoadAssetsFileFromBundle(bundle, info.Name))
                .AsReadOnly();
            var assetsFiles = blockAssetMap.Select(i => i.Value).ToArray();

            if (blockAssetMap.Count == 0)
                throw new InvalidOperationException("Bundle contains no assets files.");

            return (blockAssetMap, assetsFiles);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            throw;
        }
    }

    private static BundleFileInstance LoadAssetBundle(Stream bundleStream, AssetsManager manager)
    {
        using var activity = BundleProcessCoreActivitySources.BundleProcessCoreActivitySource
            .StartActivity();

        try
        {
            manager.LoadClassPackage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/type-tree.tpk"));

            var bundle = manager.LoadBundleFile(bundleStream, "bundle", false, true);
            if (bundle.file.DataIsCompressed)
                throw new InvalidOperationException("Bundle must be decompressed before processing.");

            if (bundle.file.BlockAndDirInfo.BlockInfos.Length == 0)
                throw new InvalidOperationException("Bundle contains no blocks.");

            return bundle;
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            throw;
        }
    }

    private Stream CreateTempStream()
    {
        var tempFolderPath = pipelineOptions.TempFolderPath ??
                             Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-bundle-process");

        if (!Directory.Exists(tempFolderPath))
            Directory.CreateDirectory(tempFolderPath);

        var tempFilePath = Path.Combine(
            tempFolderPath,
            Guid.NewGuid().ToString("N") + "-bundle.tmp"
        );

        return File.Create(tempFilePath, 4096,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Asynchronous);
    }
}