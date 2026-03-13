namespace VRChatContentPublisher.Core.Services;

public readonly record struct RpcStartupPortWarning(int ConfiguredPort, int ActivePort);

public sealed class RpcStartupPortWarningState
{
    private readonly Lock _syncRoot = new();
    private RpcStartupPortWarning? _warning;

    public void Set(int configuredPort, int activePort)
    {
        lock (_syncRoot)
        {
            _warning = new RpcStartupPortWarning(configuredPort, activePort);
        }
    }

    public bool TryConsume(out RpcStartupPortWarning warning)
    {
        lock (_syncRoot)
        {
            if (_warning is not { } current)
            {
                warning = default;
                return false;
            }

            warning = current;
            _warning = null;
            return true;
        }
    }
}