using System.Collections.Concurrent;
using System.Diagnostics;

namespace VRChatContentPublisher.VRChatApi;

/// <summary>
/// Tracks upload speed using a sliding time window with EMA smoothing.
/// </summary>
/// <remarks>
/// <para>Design principles:</para>
/// <list type="bullet">
/// <item><b>Collect/calculate decoupling</b> —
/// <see cref="RecordBytes"/> only stores samples.
/// <see cref="GetCurrentSpeed"/> recalculates speed on every call
/// and is intended to be driven by a periodic timer (e.g. every 100 ms).
/// This guarantees speed updates even when no new data arrives (stall → 0).</item>
/// <item><b>Minimum window</b> —
/// Speed is not reported until at least <see cref="MinWindowSeconds"/>
/// of sample history has accumulated, preventing inflated readings at upload start.</item>
/// <item><b>EMA smoothing</b> —
/// A raw sliding-window average is passed through an exponential moving
/// average (α = 0.3) to dampen jitter while still responding quickly.</item>
/// </list>
/// </remarks>
public sealed class UploadSpeedTracker
{
    private const double WindowSeconds = 3.0;
    private const double MinWindowSeconds = 1.0;
    private const double EmaAlpha = 0.3;

    private readonly ConcurrentQueue<SpeedSample> _samples = new();
    private readonly Lock _gate = new();

    private long _windowTotalBytes;
    private double _smoothedSpeed;

    /// <summary>
    /// Records a data point. Thread-safe — may be called concurrently from
    /// multiple chunk-upload tasks.
    /// </summary>
    public void RecordBytes(long bytes)
    {
        var now = Stopwatch.GetTimestamp();
        _samples.Enqueue(new SpeedSample(bytes, now));
        Interlocked.Add(ref _windowTotalBytes, bytes);

        // Eagerly prune to keep the queue bounded.
        PruneExpired(now);
    }

    /// <summary>
    /// Returns the current upload speed in bytes per second.
    /// Recalculates from scratch on every call; intended to be driven by a
    /// periodic timer so the value naturally decays to 0 when no data flows.
    /// </summary>
    public long GetCurrentSpeed()
    {
        var now = Stopwatch.GetTimestamp();

        lock (_gate)
        {
            PruneExpired(now);

            if (!_samples.TryPeek(out var oldest))
            {
                // No samples in the window — fully decay.
                _smoothedSpeed = 0;
                return 0;
            }

            var elapsedSeconds = (double)(now - oldest.Timestamp) / Stopwatch.Frequency;

            if (elapsedSeconds < MinWindowSeconds)
            {
                // Too little history for a reliable measurement.
                return (long)_smoothedSpeed;
            }

            var rawSpeed = Interlocked.Read(ref _windowTotalBytes) / elapsedSeconds;
            _smoothedSpeed = EmaAlpha * rawSpeed + (1 - EmaAlpha) * _smoothedSpeed;
            return (long)_smoothedSpeed;
        }
    }

    private void PruneExpired(long now)
    {
        var cutoff = now - (long)(WindowSeconds * Stopwatch.Frequency);
        while (_samples.TryPeek(out var sample) && sample.Timestamp < cutoff)
        {
            if (_samples.TryDequeue(out var removed))
                Interlocked.Add(ref _windowTotalBytes, -removed.Bytes);
        }
    }

    private readonly record struct SpeedSample(long Bytes, long Timestamp);
}
