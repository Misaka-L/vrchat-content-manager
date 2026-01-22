using System.Collections.Frozen;
using System.Collections.Immutable;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
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
        CopyToTempFile(bundleStream, "source");

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
                return bundleStream;

            // Run each processor
            foreach (var bundleProcesser in processer)
            {
                progressReporter?.Report("Running " + bundleProcesser.GetType().Name);
                bundleProcesser.Process(manager, bundle, assetsFiles, options, progressReporter);
            }

            // Write back modified assets file to bundle
            progressReporter?.Report("Writing processed assets back to bundle...");
            foreach (var blockAssetPair in blockAssetMap)
            {
                blockAssetPair.Key.SetNewData(blockAssetPair.Value.file);
            }

            var newBundleStream = CreateTempStream();
            using var writer = new AssetsFileWriter(newBundleStream, true);
            bundle.file.Write(writer);

            if (!leaveOpen)
                bundleStream.Close();

            CopyToTempFile(newBundleStream, "processed");

            newBundleStream.Position = 0;
            return newBundleStream;
        }
        finally
        {
            manager.UnloadAll();
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

    private void CopyToTempFile(Stream source, string name)
    {
        source.Position = 0;
        var tempFolderPath = pipelineOptions.TempFolderPath ??
                             Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-bundle-process");

        if (!Directory.Exists(tempFolderPath))
            Directory.CreateDirectory(tempFolderPath);

        var tempFilePath = Path.Combine(
            tempFolderPath,
            name + "-debug.tmp"
        );

        using var stream = File.Create(tempFilePath, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
        source.CopyTo(stream);
    }
}