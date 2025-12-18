using VRChatContentManager.IpcCore.Exceptions;

namespace VRChatContentManager.IpcCore;

public sealed class AppMutex : IDisposable
{
    private readonly Mutex _mutex = new(true, "Global\\VRChatContentPublisherAppMutex-89c6689a");

    private bool _isMutexOwned;

    public void OwnMutex()
    {
        _isMutexOwned = _mutex.WaitOne(0, true);
        if (!_isMutexOwned)
            throw new MutexOwnedByAnotherInstanceException();
    }

    public void Dispose()
    {
        if (_isMutexOwned)
            _mutex.ReleaseMutex();

        _mutex.Dispose();
    }
}