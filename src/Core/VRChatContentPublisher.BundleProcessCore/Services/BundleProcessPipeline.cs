using AssetsTools.NET;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Utils;

namespace VRChatContentPublisher.BundleProcessCore.Services;

internal sealed class BundleProcessPipeline(BundleProcessPipelineOptions pipelineOptions)
{
    public async ValueTask<AssetBundleFile> ProcessAsync(
        Stream bundleStream,
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Factory.StartNew(() =>
                ProcessCore(bundleStream, options, progressReporter, cancellationToken),
            cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    private AssetBundleFile ProcessCore(
        Stream bundleStream,
        BundleProcessOptions options,
        IProcessProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        progressReporter?.Report("Starting bundle processing pipeline...");

        // Load Assets Manager and Class Package
        var manager = new AssetsManager();
        try
        {
            manager.LoadClassPackage("Assets/type-tree.tpk");

            var bundle = manager.LoadBundleFile(bundleStream, "bundle", false, true);
            if (bundle.file.DataIsCompressed)
                throw new InvalidOperationException("Bundle must be decompressed before processing.");

            if (bundle.file.BlockAndDirInfo.BlockInfos.Length == 0)
                throw new InvalidOperationException("Bundle contains no blocks.");

            // Load all assets files from bundle blocks
            progressReporter?.Report("Loading assets files from bundle...");
            var blockAssetMap = bundle.file.BlockAndDirInfo.DirectoryInfos
                .Where(info => (info.Flags & 0x04) != 0)
                .Where(info =>
                {
                    var blockStream = new SegmentStream(bundle.DataStream, info.Offset, info.DecompressedSize);
                    using var reader = new AssetsFileReader(blockStream);
                    return FileTypeDetector.DetectFileType(reader, 0) == DetectedFileType.AssetsFile;
                })
                .ToDictionary(info => info, info => manager.LoadAssetsFileFromBundle(bundle, info.Name))
                .AsReadOnly();
            var assetsFiles = blockAssetMap.Select(i => i.Value).ToArray();

            if (blockAssetMap.Count == 0)
                throw new InvalidOperationException("Bundle contains no assets files.");

            manager.LoadClassDatabaseFromPackage(blockAssetMap.First().Value.file.Metadata.UnityVersion);

            // Determine which processors should run or should bundle processing be skipped
            progressReporter?.Report("Determining applicable processors...");
            var processer = options.Processers
                .Where(p => p.ShouldProcess(manager, bundle, assetsFiles, options, progressReporter))
                .ToArray();

            if (processer.Length == 0)
                return bundle.file;

            // Run each processor
            foreach (var bundleProcesser in processer)
            {
                progressReporter?.Report("Running " + bundleProcesser.GetType().Name);
                bundleProcesser.Process(manager, bundle, assetsFiles, options, progressReporter);
            }

            // Set new data to bundle blocks
            foreach (var blockAssetPair in blockAssetMap)
            {
                blockAssetPair.Key.SetNewData(blockAssetPair.Value.file);
            }

            return bundle.file;
        }
        catch
        {
            manager.UnloadAllAssetsFiles(true);
            throw;
        }
        finally
        {
            manager.UnloadClassDatabase();
            manager.UnloadClassPackage();
            manager.UnloadAllAssetsFiles(true);
            manager.MonoTempGenerator?.Dispose();
        }
    }
}