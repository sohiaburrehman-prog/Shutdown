using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ITimerService _timerService;
    private readonly IProcessMonitorService _processMonitorService;
    private readonly IIdleDetectionService _idleDetectionService;
    private readonly ISystemActionService _actionService;
    private readonly ISettingsService _settingsService;
    private readonly IPowerService _powerService;

    // ── Timer status ──
    [ObservableProperty] private string timerDisplay = "--:--:--";
    [ObservableProperty] private string timerStatusText = "No active timer";
    [ObservableProperty] private double timerProgress;
    [ObservableProperty] private bool isTimerActive;
    [ObservableProperty] private string nextActionTitle = "Nothing scheduled";
    [ObservableProperty] private string nextActionDetail = "Create a timer, schedule, process monitor, or idle rule to automate your next power action.";
    [ObservableProperty] private string nextActionTrigger = "No trigger selected";
    [ObservableProperty] private string automationSummary = "No active automations";
    [ObservableProperty] private string batteryInsight = "Power state unavailable";
    [ObservableProperty] private bool hasActiveAutomations;
    [ObservableProperty] private bool canCancelNextAction;
    [ObservableProperty] private ObservableCollection<AutomationStatus> activeAutomations = new();

    // ── Process monitor status ──
    [ObservableProperty] private string processStatusText = "Not monitoring";
    [ObservableProperty] private bool isProcessMonitoring;

    // ── Idle status ──
    [ObservableProperty] private string idleStatusText = "Not monitoring";
    [ObservableProperty] private string currentIdleDisplay = "0:00";
    [ObservableProperty] private bool isIdleMonitoring;

    [ObservableProperty] private int activeScheduleCount;
    [ObservableProperty] private string nextScheduleText = "No schedules";

    // ── Battery status ──
    [ObservableProperty] private double batteryPercentage = 100;
    [ObservableProperty] private string batteryStatusText = "Unknown";
    [ObservableProperty] private bool isCharging;
    [ObservableProperty] private string batteryIcon = "\uEBAA"; // Battery Unknown

    /// <summary>
    /// Raised when a quick action needs user confirmation.
    /// </summary>
    public event Action<TimerAction>? ConfirmQuickAction;

    public DashboardViewModel(
        ITimerService timerService,
        IProcessMonitorService processMonitorService,
        IIdleDetectionService idleDetectionService,
        ISystemActionService actionService,
        ISettingsService settingsService,
        IPowerService powerService)
    {
        _timerService = timerService;
        _processMonitorService = processMonitorService;
        _idleDetectionService = idleDetectionService;
        _actionService = actionService;
        _settingsService = settingsService;
        _powerService = powerService;

        _timerService.Tick += OnTimerTick;
        _timerService.Completed += OnTimerCompleted;
        _processMonitorService.MonitorTick += OnProcessTick;
        _idleDetectionService.IdleTick += OnIdleTick;
        _powerService.BatteryStatusChanged += OnBatteryChanged;
    }

    public void Refresh()
    {
        // Timer
        IsTimerActive = _timerService.State == TimerState.Running || _timerService.State == TimerState.Paused;
        if (IsTimerActive)
        {
            TimerDisplay = _timerService.Remaining.ToString(@"hh\:mm\:ss");
            TimerStatusText = _timerService.State == TimerState.Paused ? "Paused" : "Running";
        }
        else
        {
            TimerDisplay = "--:--:--";
            TimerStatusText = "No active timer";
            TimerProgress = 0;
        }

        // Process
        IsProcessMonitoring = _processMonitorService.IsMonitoring;
        ProcessStatusText = _processMonitorService.IsMonitoring
            ? $"Watching: {_processMonitorService.MonitoredProcess?.ProcessName ?? "Unknown"}"
            : "Not monitoring";

        // Idle
        IsIdleMonitoring = _idleDetectionService.IsMonitoring;
        IdleStatusText = _idleDetectionService.IsMonitoring ? "Monitoring" : "Not monitoring";

        // Schedules
        var schedules = _settingsService.Settings.Schedules;
        ActiveScheduleCount = schedules.Count(s => s.IsEnabled);
        var next = schedules.Where(s => s.IsEnabled && s.NextRun.HasValue)
            .OrderBy(s => s.NextRun).FirstOrDefault();
        NextScheduleText = next?.NextRun != null
            ? $"Next: {next.NextRun:ddd HH:mm}"
            : "No upcoming";

        // Battery
        UpdateBatteryUI(_powerService.RemainingPercentage, _powerService.IsCharging);

        UpdateCommandCenter();
    }

    private void UpdateCommandCenter()
    {
        var automations = new List<AutomationStatus>();

        if (IsTimerActive)
        {
            automations.Add(new AutomationStatus
            {
                Name = "Countdown",
                Detail = $"{TimerStatusText} with {TimerDisplay} remaining",
                Icon = "\uE916",
                Accent = "Timer"
            });
        }

        if (IsProcessMonitoring)
        {
            automations.Add(new AutomationStatus
            {
                Name = "Process Monitor",
                Detail = ProcessStatusText,
                Icon = "\uE773",
                Accent = "Process"
            });
        }

        if (IsIdleMonitoring)
        {
            automations.Add(new AutomationStatus
            {
                Name = "Idle Detection",
                Detail = $"{IdleStatusText}. Current idle: {CurrentIdleDisplay}",
                Icon = "\uEC46",
                Accent = "Idle"
            });
        }

        foreach (var schedule in _settingsService.Settings.Schedules
                     .Where(s => s.IsEnabled)
                     .OrderBy(s => s.NextRun)
                     .Take(3))
        {
            automations.Add(new AutomationStatus
            {
                Name = schedule.IsOneTime ? "One-time Schedule" : "Recurring Schedule",
                Detail = schedule.NextRun.HasValue
                    ? $"{schedule.Action} at {schedule.NextRun:ddd, MMM d HH:mm}"
                    : $"{schedule.Action}: {schedule.FriendlyDescription}",
                Icon = "\uE787",
                Accent = "Schedule"
            });
        }

        if (_settingsService.Settings.LowBatteryAutomationEnabled)
        {
            automations.Add(new AutomationStatus
            {
                Name = "Low Battery",
                Detail = $"{_settingsService.Settings.LowBatteryAction} at {_settingsService.Settings.LowBatteryThreshold}% battery",
                Icon = "\uEBA6",
                Accent = "Battery"
            });
        }

        if (_settingsService.Settings.CriticalBatteryAutomationEnabled)
        {
            automations.Add(new AutomationStatus
            {
                Name = "Critical Battery",
                Detail = $"{_settingsService.Settings.CriticalBatteryAction} at {_settingsService.Settings.CriticalBatteryThreshold}% battery",
                Icon = "\uEBA5",
                Accent = "Battery"
            });
        }

        ActiveAutomations = new ObservableCollection<AutomationStatus>(automations);
        HasActiveAutomations = automations.Count > 0;
        AutomationSummary = HasActiveAutomations
            ? $"{automations.Count} automation{(automations.Count == 1 ? "" : "s")} watching your PC"
            : "No active automations";

        var nextSchedule = _settingsService.Settings.Schedules
            .Where(s => s.IsEnabled && s.NextRun.HasValue)
            .OrderBy(s => s.NextRun)
            .FirstOrDefault();

        if (IsTimerActive)
        {
            NextActionTitle = $"{_timerService.State}: timer ends in {TimerDisplay}";
            NextActionDetail = "The selected timer action will run after the warning countdown, with cancel and postpone controls available.";
            NextActionTrigger = "Trigger: Countdown timer";
        }
        else if (nextSchedule?.NextRun is DateTime nextRun)
        {
            NextActionTitle = $"{nextSchedule.Action} on {nextRun:ddd, MMM d}";
            NextActionDetail = $"Scheduled for {nextRun:HH:mm}. Keep the app running or minimized to tray so this automation can fire.";
            NextActionTrigger = $"Trigger: {nextSchedule.FriendlyDescription}";
        }
        else if (IsProcessMonitoring)
        {
            NextActionTitle = "Waiting on a process";
            NextActionDetail = ProcessStatusText;
            NextActionTrigger = "Trigger: Process monitor";
        }
        else if (IsIdleMonitoring)
        {
            NextActionTitle = "Watching for idle time";
            NextActionDetail = $"{IdleStatusText}. Current idle time is {CurrentIdleDisplay}.";
            NextActionTrigger = "Trigger: Idle detection";
        }
        else if (_settingsService.Settings.CriticalBatteryAutomationEnabled || _settingsService.Settings.LowBatteryAutomationEnabled)
        {
            NextActionTitle = "Battery automation armed";
            NextActionDetail = BatteryInsight;
            NextActionTrigger = "Trigger: Battery threshold";
        }
        else
        {
            NextActionTitle = "Nothing scheduled";
            NextActionDetail = "Start with a countdown, schedule a bedtime shutdown, or watch a long-running process.";
            NextActionTrigger = "No trigger selected";
        }

        CanCancelNextAction = IsTimerActive || _actionService.IsWarningActive;
    }

    private void UpdateBatteryUI(double percentage, bool charging)
    {
        BatteryPercentage = percentage;
        IsCharging = charging;
        BatteryStatusText = charging ? "Charging" : "Discharging";
        BatteryInsight = charging
            ? $"Plugged in at {percentage:0}%"
            : percentage <= 20
                ? $"Battery is low at {percentage:0}%. Consider Sleep or Hibernate."
                : $"On battery at {percentage:0}%. Use automations to save power.";
        
        // Pick icon based on level
        if (charging) BatteryIcon = "\uEBB3"; // Charging
        else if (percentage > 90) BatteryIcon = "\uEBA0"; // 100
        else if (percentage > 70) BatteryIcon = "\uEBA9"; // 80
        else if (percentage > 50) BatteryIcon = "\uEBA8"; // 60
        else if (percentage > 30) BatteryIcon = "\uEBA7"; // 40
        else if (percentage > 10) BatteryIcon = "\uEBA6"; // 20
        else BatteryIcon = "\uEBA5"; // Critical
    }

    private void OnBatteryChanged(double percentage, bool charging)
    {
        UpdateBatteryUI(percentage, charging);
    }

    // ── Quick actions (request confirmation from view) ──
    [RelayCommand]
    private void QuickSleep() => ConfirmQuickAction?.Invoke(TimerAction.Sleep);

    [RelayCommand]
    private void QuickRestart() => ConfirmQuickAction?.Invoke(TimerAction.Restart);

    [RelayCommand]
    private void QuickShutdown() => ConfirmQuickAction?.Invoke(TimerAction.Shutdown);

    [RelayCommand]
    private void QuickLock() => ConfirmQuickAction?.Invoke(TimerAction.Lock);

    [RelayCommand]
    private void CancelNextAction()
    {
        _timerService.Cancel();
        _actionService.CancelWarning();
        Refresh();
    }

    /// <summary>
    /// Called by the view after the user confirms a quick action.
    /// </summary>
    public void ExecuteQuickAction(TimerAction action) =>
        _actionService.ExecuteWithWarning(action, "Dashboard Quick Action", $"Quick {action} from Dashboard");

    private void OnTimerTick(TimeSpan remaining)
    {
        TimerDisplay = remaining.ToString(@"hh\:mm\:ss");
        IsTimerActive = true;
        TimerStatusText = "Running";
        UpdateCommandCenter();
    }

    private void OnTimerCompleted()
    {
        IsTimerActive = false;
        TimerDisplay = "00:00:00";
        TimerStatusText = "Completed";
        UpdateCommandCenter();
    }

    private void OnProcessTick(TimeSpan elapsed)
    {
        ProcessStatusText = $"Watching: {_processMonitorService.MonitoredProcess?.ProcessName} ({elapsed:hh\\:mm\\:ss})";
        UpdateCommandCenter();
    }

    private void OnIdleTick(TimeSpan idle)
    {
        CurrentIdleDisplay = idle.ToString(@"m\:ss");
        UpdateCommandCenter();
    }
}

public class AutomationStatus
{
    public string Name { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Icon { get; set; } = "\uE8A5";
    public string Accent { get; set; } = "";
}
