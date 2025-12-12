using System.Diagnostics;

namespace VRChatContentManager.Core.Utils;

public sealed class StopwatchScope : IDisposable
{
    private readonly Stopwatch _stopwatch = new();

    private readonly Action<StopwatchScope>? _stopAction;

    public StopwatchScope(Action<StopwatchScope>? stopAction = null)
    {
        _stopAction = stopAction;
    }

    public static StopwatchScope Enter(Action<StopwatchScope>? stopAction = null)
    {
        var stopWatchScope = new StopwatchScope(stopAction);
        stopWatchScope.Start();

        return stopWatchScope;
    }

    public void Start() => _stopwatch.Restart();

    public void Stop()
    {
        if (!_stopwatch.IsRunning)
            return;

        _stopwatch.Stop();
        _stopAction?.Invoke(this);
    }

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public long ElapsedTicks => _stopwatch.ElapsedTicks;

    public void Dispose()
    {
        Stop();
    }
}