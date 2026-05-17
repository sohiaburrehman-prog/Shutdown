using Microsoft.UI.Dispatching;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public interface ITimerService
{
    event Action<TimeSpan>? Tick;
    event Action? Completed;

    /// <summary>
    /// Raised when the system resumes from sleep/hibernate and the timer had already expired.
    /// The bool parameter is true if the timer expired while asleep.
    /// </summary>
    event Action<bool>? SleepWakeDetected;

    TimerState State { get; }
    TimeSpan Remaining { get; }

    void Start(TimeSpan duration);
    void Pause();
    void Resume();
    void Cancel();
}

public class TimerService : ITimerService
{
    private readonly DispatcherQueueTimer _timer;
    private readonly ITaskbarService _taskbarService;
    private DateTime _endTime;
    private TimeSpan _totalDuration;
    private TimeSpan _pausedRemaining;
    private DateTime _lastTickTime;

    public event Action<TimeSpan>? Tick;
    public event Action? Completed;
    public event Action<bool>? SleepWakeDetected;

    public TimerState State { get; private set; } = TimerState.Idle;
    public TimeSpan Remaining { get; private set; }

    public TimerService(ITaskbarService taskbarService)
    {
        _taskbarService = taskbarService;
        var queue = DispatcherQueue.GetForCurrentThread();
        _timer = queue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250);
        _timer.IsRepeating = true;
        _timer.Tick += OnTick;
    }

    public void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;

        _totalDuration = duration;
        _endTime = DateTime.UtcNow + duration;
        Remaining = duration;
        State = TimerState.Running;
        _lastTickTime = DateTime.UtcNow;
        _timer.Start();

        _taskbarService.SetState(Win32.NativeMethods.TBPFLAG.TBPF_NORMAL);
    }

    public void Pause()
    {
        if (State != TimerState.Running) return;

        _timer.Stop();
        _pausedRemaining = _endTime - DateTime.UtcNow;
        if (_pausedRemaining < TimeSpan.Zero) _pausedRemaining = TimeSpan.Zero;
        Remaining = _pausedRemaining;
        State = TimerState.Paused;

        _taskbarService.SetState(Win32.NativeMethods.TBPFLAG.TBPF_PAUSED);
    }

    public void Resume()
    {
        if (State != TimerState.Paused) return;

        _endTime = DateTime.UtcNow + _pausedRemaining;
        State = TimerState.Running;
        _lastTickTime = DateTime.UtcNow;
        _timer.Start();

        _taskbarService.SetState(Win32.NativeMethods.TBPFLAG.TBPF_NORMAL);
    }

    public void Cancel()
    {
        _timer.Stop();
        Remaining = TimeSpan.Zero;
        State = TimerState.Cancelled;
        _taskbarService.Reset();
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        var now = DateTime.UtcNow;

        // Detect sleep/wake: if more than 5 seconds passed since last tick (timer fires every 250ms),
        // the system likely went to sleep and woke back up.
        var elapsed = now - _lastTickTime;
        if (elapsed.TotalSeconds > 5 && State == TimerState.Running)
        {
            bool expiredDuringSleep = now >= _endTime;
            SleepWakeDetected?.Invoke(expiredDuringSleep);

            if (expiredDuringSleep)
            {
                // Timer expired while system was asleep — fire completion immediately
                Remaining = TimeSpan.Zero;
                _timer.Stop();
                State = TimerState.Completed;
                Tick?.Invoke(Remaining);
                Completed?.Invoke();
                return;
            }
        }
        _lastTickTime = now;

        Remaining = _endTime - now;

        if (Remaining <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            _timer.Stop();
            State = TimerState.Completed;
            _taskbarService.Reset();
            Completed?.Invoke();
        }

        if (_totalDuration.TotalSeconds > 0)
        {
            var progress = (double)(_totalDuration.TotalSeconds - Remaining.TotalSeconds) / _totalDuration.TotalSeconds;
            _taskbarService.SetProgress(progress);
        }

        Tick?.Invoke(Remaining);
    }
}
