using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Extensions.Logging;
using VRChatContentManager.ConnectCore.Services;
using VRChatContentManager.Core.Services.App;
using VRChatContentManager.Core.Services.PublishTask.ContentPublisher;

namespace VRChatContentManager.Core.Services.PublishTask;

public sealed class ContentPublishTaskService
{
    private readonly HttpClient _awsHttpClient;
    private readonly IFileService _tempFileService;
    private readonly IContentPublisher _contentPublisher;
    
    private readonly ILogger<ContentPublishTaskService> _logger;

    private readonly string _contentId;
    private readonly string _bundleFileId;

    internal ContentPublishTaskService(
        string contentId, string bundleFileId,
        HttpClient awsHttpClient, IFileService tempFileService,
        IContentPublisher contentPublisher, ILogger<ContentPublishTaskService> logger)
    {
        _contentId = contentId;
        _bundleFileId = bundleFileId;

        _awsHttpClient = awsHttpClient;
        _tempFileService = tempFileService;
        _contentPublisher = contentPublisher;
        _logger = logger;
    }

    public async ValueTask StartTaskAsync()
    {
        try
        {
            // Step 1: Decompress (if needed) and recompress the bundle file.
            _logger.LogInformation("Starting publish task for content {ContentId}", _contentId);

            _logger.LogInformation("Pre Processing bundle file {BundleFileId}", _bundleFileId);
            await using var fileStream = await _tempFileService.GetFileAsync(_bundleFileId);
            if (fileStream is null)
                throw new InvalidOperationException("Bundle file with provided id is not found.");

            var tempBundleFilePath = GetTempBundleFilePath();

            _logger.LogInformation("Compressing bundle file to temporary path {TempBundleFilePath}",
                tempBundleFilePath);
            await using var tempBundleFileStream =
                File.Create(tempBundleFilePath, 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);

            await CompressBundleAsync(fileStream, tempBundleFileStream);

            _logger.LogInformation("Compression completed, preparing to publish.");
            // Step 2: Publish the content using the provided content publisher.
            await _contentPublisher.PublishAsync(tempBundleFileStream, _awsHttpClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing bundle file {BundleFileId}", _bundleFileId);
        }
    }

    private Task CompressBundleAsync(Stream bundleStream, Stream outputStream)
    {
        return Task.Factory.StartNew(() =>
        {
            var bundleFile = new AssetBundleFile();

            // Do no dispose reader, or it will dispose the bundle file stream.
            var bundleReader = new AssetsFileReader(bundleStream);
            bundleFile.Read(bundleReader);

            if (bundleFile.DataIsCompressed)
            {
                var newBundleFile = BundleHelper.UnpackBundle(bundleFile);
                bundleFile.Close();
                bundleFile = newBundleFile;
            }

            // Do no dispose writer, or it will dispose the bundle file stream.
            var writer = new AssetsFileWriter(outputStream);
            bundleFile.Pack(writer, AssetBundleCompressionType.LZMA);
            bundleFile.Close();

            outputStream.Position = 0;
        }, TaskCreationOptions.LongRunning);
    }
    
    private string GetTempBundleFilePath()
    {
        var tempBundlePath = Path.Combine(AppStorageService.GetTempPath(), "temp-bundle");
        if (!Directory.Exists(tempBundlePath))
            Directory.CreateDirectory(tempBundlePath);
        
        return Path.Combine(tempBundlePath, $"{_contentId}-{Guid.NewGuid():N}");
    }
}

public sealed class ContentPublishTaskFactory(
    HttpClient awsHttpClient,
    IFileService tempFileService,
    ILogger<ContentPublishTaskService> logger)
{
    public ValueTask<ContentPublishTaskService> Create(string contentId, string bundleFileId,
        IContentPublisher contentPublisher)
    {
        var publishTask = new ContentPublishTaskService(
            contentId,
            bundleFileId,
            awsHttpClient,
            tempFileService,
            contentPublisher,
            logger
        );

        return ValueTask.FromResult(publishTask);
    }
}