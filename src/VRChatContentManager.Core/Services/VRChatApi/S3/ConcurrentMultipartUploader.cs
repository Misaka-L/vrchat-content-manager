using Microsoft.Extensions.Logging;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Files;

namespace VRChatContentManager.Core.Services.VRChatApi.S3;

public sealed class ConcurrentMultipartUploader(
    VRChatApiClient apiClient,
    HttpClient awsClient,
    ILogger<ConcurrentMultipartUploader> logger,
    Stream fileStream,
    string fileId,
    int fileVersion,
    VRChatApiFileType fileType)
{
    private const long ChunkSize = 100 * 1024 * 1024; // 100 MB

    private readonly Lock _chunkCreationLock = new();
    private int _lastPartNumber;

    private readonly List<UploadChunk> _chunks = [];
    private readonly Dictionary<int, string> _eTags = [];

    private readonly TaskCompletionSource<string[]> _allChunksUploadedTcs = new();

    public async Task<string[]> UploadAsync()
    {
        logger.LogInformation("Starting upload of file {FileId} version {FileVersion} in chunks of {ChunkSize} bytes",
            fileId, fileVersion, ChunkSize);

        _ = Task.Factory.StartNew(CreateChunks, TaskCreationOptions.LongRunning);
        return await _allChunksUploadedTcs.Task;
    }

    private void CreateChunks()
    {
        _chunkCreationLock.Enter();

        var completedChunks = _chunks.Where(c => c.IsCompleted).ToArray();
        if (completedChunks.Length > 0)
        {
            foreach (var uploadChunk in _chunks)
            {
                _eTags[uploadChunk.PartNumber] = uploadChunk.ETag;
            }

            _chunks.RemoveAll(chunk => chunk.IsCompleted);
        }

        while (_chunks.Count < 2)
        {
            var chunk = CreateChunk();
            if (chunk is null)
            {
                logger.LogInformation("All chunks created for file {FileId} version {FileVersion}", fileId,
                    fileVersion);

                if (_chunks.Count != 0 && _chunks.Any(c => !c.IsCompleted))
                    return;

                var eTags = _eTags.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
                _allChunksUploadedTcs.SetResult(eTags);
                return;
            }

            _chunks.Add(chunk);
        }

        _chunkCreationLock.Exit();
    }

    private UploadChunk? CreateChunk()
    {
        var buffer = new byte[ChunkSize];
        var bytesRead = fileStream.Read(buffer);

        if (bytesRead == 0)
            return null;

        _lastPartNumber++;
        Thread.Sleep(TimeSpan.FromSeconds(Random.Shared.Next(2, 5)));
        var uploadUrl = apiClient.GetFilePartUploadUrlAsync(fileId, fileVersion, _lastPartNumber, fileType)
            .AsTask().GetAwaiter().GetResult();

        logger.LogInformation("Creating upload chunk {PartNumber} Size {Size}MiB for file {FileId} version {FileVersion}",
            _lastPartNumber, bytesRead / 1024d / 1024d, fileId, fileVersion);
        var chunk = new UploadChunk(_lastPartNumber, buffer, bytesRead, uploadUrl, awsClient, CreateChunks);
        _ = Task.Factory.StartNew(async () =>
        {
            try
            {
                await chunk.UploadAsync();
            }
            catch (Exception e)
            {
                _allChunksUploadedTcs.SetException(e);
                throw;
            }
        }, TaskCreationOptions.LongRunning);

        return chunk;
    }

    private sealed class UploadChunk(
        int partNumber,
        byte[] buffer,
        int bufferSize,
        string uploadUrl,
        HttpClient awsClient,
        Action completedCallback)
    {
        public int PartNumber => partNumber;
        public bool IsCompleted { get; private set; }
        public string ETag { get; private set; } = string.Empty;

        public async Task UploadAsync()
        {
            using var content = new ByteArrayContent(buffer, 0, bufferSize);
            var response = await awsClient.PutAsync(uploadUrl, content);

            response.EnsureSuccessStatusCode();
            IsCompleted = true;

            if (response.Headers.ETag is null)
                throw new UnexpectedApiBehaviourException("Api did not return an ETag header.");

            ETag = response.Headers.ETag.Tag.Trim('\"', '\'');

            completedCallback();
        }
    }
}

public sealed class ConcurrentMultipartUploaderFactory(ILogger<ConcurrentMultipartUploader> logger)
{
    public ConcurrentMultipartUploader Create(Stream fileStream, string fileId, int fileVersion,
        VRChatApiFileType fileType, VRChatApiClient apiClient, HttpClient awsClient)
    {
        return new ConcurrentMultipartUploader(apiClient, awsClient, logger, fileStream, fileId, fileVersion, fileType);
    }
}