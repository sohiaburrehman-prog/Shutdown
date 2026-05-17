namespace ShutdownTimer.Tests;

/// <summary>
/// Tests for the rate-limiting (cooldown) logic in SystemActionService.
/// Extracted as pure logic to avoid WinUI/Win32 dependencies.
/// </summary>
[TestClass]
public class CooldownTests
{
    // Mirrors SystemActionService's cooldown logic
    private static bool IsCooldownActive(DateTime lastActionTime, TimeSpan cooldown)
        => DateTime.UtcNow - lastActionTime < cooldown;

    [TestMethod]
    public void Cooldown_WhenNoActionYet_ShouldNotBeActive()
    {
        var lastAction = DateTime.MinValue; // never fired
        Assert.IsFalse(IsCooldownActive(lastAction, TimeSpan.FromSeconds(60)));
    }

    [TestMethod]
    public void Cooldown_WhenJustFired_ShouldBeActive()
    {
        var lastAction = DateTime.UtcNow; // fired right now
        Assert.IsTrue(IsCooldownActive(lastAction, TimeSpan.FromSeconds(60)));
    }

    [TestMethod]
    public void Cooldown_WhenExpired_ShouldNotBeActive()
    {
        var lastAction = DateTime.UtcNow.AddSeconds(-61); // fired 61s ago
        Assert.IsFalse(IsCooldownActive(lastAction, TimeSpan.FromSeconds(60)));
    }

    [TestMethod]
    public void Cooldown_WhenHalfExpired_ShouldStillBeActive()
    {
        var lastAction = DateTime.UtcNow.AddSeconds(-30); // fired 30s ago, cooldown is 60s
        Assert.IsTrue(IsCooldownActive(lastAction, TimeSpan.FromSeconds(60)));
    }
}

/// <summary>
/// Tests for the timer state machine logic (mirrors TimerService state transitions).
/// Tests pure logic without WinUI DispatcherQueue dependency.
/// </summary>
[TestClass]
public class TimerStateTests
{
    // Mirrors TimerService state management without WinUI dependencies
    private enum TimerState { Idle, Running, Paused, Completed, Cancelled }

    private TimerState _state = TimerState.Idle;
    private TimeSpan _duration;
    private TimeSpan _remaining;

    private void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;
        _duration = duration;
        _remaining = duration;
        _state = TimerState.Running;
    }

    private void Pause()
    {
        if (_state != TimerState.Running) return;
        _state = TimerState.Paused;
    }

    private void Resume()
    {
        if (_state != TimerState.Paused) return;
        _state = TimerState.Running;
    }

    private void Cancel()
    {
        _remaining = TimeSpan.Zero;
        _state = TimerState.Cancelled;
    }

    private void Complete()
    {
        _remaining = TimeSpan.Zero;
        _state = TimerState.Completed;
    }

    [TestMethod]
    public void Timer_InitialState_IsIdle()
        => Assert.AreEqual(TimerState.Idle, _state);

    [TestMethod]
    public void Timer_Start_SetsRunning()
    {
        Start(TimeSpan.FromMinutes(5));
        Assert.AreEqual(TimerState.Running, _state);
    }

    [TestMethod]
    public void Timer_StartWithZeroDuration_RemainsIdle()
    {
        Start(TimeSpan.Zero);
        Assert.AreEqual(TimerState.Idle, _state);
    }

    [TestMethod]
    public void Timer_Pause_SetsPaused()
    {
        Start(TimeSpan.FromMinutes(5));
        Pause();
        Assert.AreEqual(TimerState.Paused, _state);
    }

    [TestMethod]
    public void Timer_ResumeFromPaused_SetsRunning()
    {
        Start(TimeSpan.FromMinutes(5));
        Pause();
        Resume();
        Assert.AreEqual(TimerState.Running, _state);
    }

    [TestMethod]
    public void Timer_PauseWhileIdle_HasNoEffect()
    {
        Pause(); // should be a no-op
        Assert.AreEqual(TimerState.Idle, _state);
    }

    [TestMethod]
    public void Timer_Cancel_SetsCancelledAndZeroRemaining()
    {
        Start(TimeSpan.FromMinutes(5));
        Cancel();
        Assert.AreEqual(TimerState.Cancelled, _state);
        Assert.AreEqual(TimeSpan.Zero, _remaining);
    }

    [TestMethod]
    public void Timer_Complete_SetsCompletedAndZeroRemaining()
    {
        Start(TimeSpan.FromMinutes(5));
        Complete();
        Assert.AreEqual(TimerState.Completed, _state);
        Assert.AreEqual(TimeSpan.Zero, _remaining);
    }

    [TestMethod]
    public void Timer_ProgressPercent_ZeroAtStart()
    {
        Start(TimeSpan.FromMinutes(5));
        var progress = _duration.TotalSeconds > 0
            ? (1.0 - _remaining.TotalSeconds / _duration.TotalSeconds) * 100
            : 0;
        Assert.AreEqual(0.0, progress, 0.001);
    }

    [TestMethod]
    public void Timer_ProgressPercent_HundredAtComplete()
    {
        Start(TimeSpan.FromMinutes(5));
        _remaining = TimeSpan.Zero;
        var progress = _duration.TotalSeconds > 0
            ? (1.0 - _remaining.TotalSeconds / _duration.TotalSeconds) * 100
            : 0;
        Assert.AreEqual(100.0, progress, 0.001);
    }
}

/// <summary>
/// Tests for pre-action program path validation logic.
/// </summary>
[TestClass]
public class PreActionPathValidationTests
{
    private static bool IsPathAllowed(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var trimmed = path.Trim();

        // Reject UNC/network paths
        if (trimmed.StartsWith(@"\\") || trimmed.StartsWith("//"))
            return false;

        // Reject scripts / batch files — only .exe and .com allowed
        var ext = Path.GetExtension(trimmed).ToLowerInvariant();
        if (ext is not (".exe" or ".com"))
            return false;

        return true;
    }

    [TestMethod]
    public void PathValidation_NullPath_IsNotAllowed()
        => Assert.IsFalse(IsPathAllowed(null));

    [TestMethod]
    public void PathValidation_EmptyPath_IsNotAllowed()
        => Assert.IsFalse(IsPathAllowed(""));

    [TestMethod]
    public void PathValidation_UncPath_IsNotAllowed()
        => Assert.IsFalse(IsPathAllowed(@"\\server\share\tool.exe"));

    [TestMethod]
    public void PathValidation_BatchScript_IsNotAllowed()
        => Assert.IsFalse(IsPathAllowed(@"C:\tools\cleanup.bat"));

    [TestMethod]
    public void PathValidation_PowerShellScript_IsNotAllowed()
        => Assert.IsFalse(IsPathAllowed(@"C:\tools\script.ps1"));

    [TestMethod]
    public void PathValidation_ValidExe_IsAllowed()
        => Assert.IsTrue(IsPathAllowed(@"C:\tools\backup.exe"));

    [TestMethod]
    public void PathValidation_ComExtension_IsAllowed()
        => Assert.IsTrue(IsPathAllowed(@"C:\windows\cmd.com"));

    [TestMethod]
    public void PathValidation_CaseInsensitiveExtension_IsAllowed()
        => Assert.IsTrue(IsPathAllowed(@"C:\tools\BACKUP.EXE"));
}
