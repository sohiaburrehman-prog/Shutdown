using Microsoft.UI.Dispatching;
using ShutdownTimer.Win32;

namespace ShutdownTimer.Services;

public interface IIdleDetectionService
{
    event Action? IdleThresholdReached;
    event Action<TimeSpan>? IdleTick;

    bool IsMonitoring { get; }
    TimeSpan CurrentIdleTime { get; }

    void StartMonitoring(int idleMinutes);
    void StopMonitoring();
}

public class IdleDetectionService : IIdleDetectionService
{
    private CancellationTokenSource? _cts;
    private readonly DispatcherQueue _dispatcher;

    public event Action? IdleThresholdReached;
    public event Action<TimeSpan>? IdleTick;

    public bool IsMonitoring { get; private set; }
    public TimeSpan CurrentIdleTime { get; private set; }

    public IdleDetectionService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public void StartMonitoring(int idleMinutes)
    {
        StopMonitoring();

        IsMonitoring = true;
        _cts = new CancellationTokenSource();
        _ = PollIdleAsync(TimeSpan.FromMinutes(idleMinutes), _cts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    System.Diagnostics.Debug.WriteLine($"[IdleDetectionService] PollIdleAsync failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        IsMonitoring = false;
        CurrentIdleTime = TimeSpan.Zero;
    }

    private async Task PollIdleAsync(TimeSpan threshold, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(3000, ct);

                var idle = NativeMethods.GetIdleTime();
                CurrentIdleTime = idle;

                _dispatcher.TryEnqueue(() => IdleTick?.Invoke(idle));

                if (idle >= threshold)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        IsMonitoring = false;
                        IdleThresholdReached?.Invoke();
                    });
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
