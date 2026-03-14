namespace VRChatContentPublisher.Core;

public sealed class SimpleSemaphoreSlimLockScope : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;

    private SimpleSemaphoreSlimLockScope(SemaphoreSlim semaphoreSlim)
    {
        _semaphoreSlim = semaphoreSlim;
    }

    public static async Task<SimpleSemaphoreSlimLockScope> WaitAsync(
        SemaphoreSlim semaphoreSlim,
        CancellationToken cancellationToken = default
    )
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return new SimpleSemaphoreSlimLockScope(semaphoreSlim);
    }

    public void Dispose()
    {
        _semaphoreSlim.Release();
    }
}