namespace ShutdownTimer.Models;

public class AppSettings
{
    /// <summary>
    /// Settings schema version. Increment when adding/removing/renaming properties
    /// so the migration path in SettingsService can handle upgrades gracefully.
    /// </summary>
    public int SettingsVersion { get; set; } = 4;

    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool ShowWarningNotification { get; set; } = true;
    public bool ShowToastNotification { get; set; } = true;
    public int WarningSeconds { get; set; } = 30;
    public TimerAction DefaultAction { get; set; } = TimerAction.Shutdown;
    public int ThemeIndex { get; set; } = 0; // 0 = System, 1 = Light, 2 = Dark
    public bool RunAtStartup { get; set; } = false;
    public bool EnableNotificationSound { get; set; } = true;
    public bool LowBatteryAutomationEnabled { get; set; } = false;
    public int LowBatteryThreshold { get; set; } = 20;
    public TimerAction LowBatteryAction { get; set; } = TimerAction.Sleep;
    public bool CriticalBatteryAutomationEnabled { get; set; } = false;
    public int CriticalBatteryThreshold { get; set; } = 10;
    public TimerAction CriticalBatteryAction { get; set; } = TimerAction.Hibernate;
    public bool BatteryAutomationOnlyWhenUnplugged { get; set; } = true;
    public WindowState Window { get; set; } = new();
    public List<ScheduleEntry> Schedules { get; set; } = new();

    /// <summary>
    /// Configurable quick presets for the Countdown Timer page (e.g. "15m", "30m", "1h", "2h").
    /// </summary>
    public List<PresetEntry> QuickPresets { get; set; } = new()
    {
        new PresetEntry { Label = "15 min", TotalMinutes = 15 },
        new PresetEntry { Label = "30 min", TotalMinutes = 30 },
        new PresetEntry { Label = "1 hour", TotalMinutes = 60 },
        new PresetEntry { Label = "2 hours", TotalMinutes = 120 },
    };

    /// <summary>
    /// Optional path to a program (.exe or .com) to run before the system action executes.
    /// </summary>
    public string? PreActionProgramPath { get; set; }

    /// <summary>
    /// Seconds to wait for the pre-action program to finish before proceeding.
    /// </summary>
    public int PreActionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Activity history log entries (most recent first).
    /// </summary>
    public List<HistoryEntry> History { get; set; } = new();
}

public class WindowState
{
    public double Width { get; set; } = 700;
    public double Height { get; set; } = 550;
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
}

public class PresetEntry
{
    public string Label { get; set; } = "";
    public int TotalMinutes { get; set; }
}

public class HistoryEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public TimerAction Action { get; set; }
    public string Trigger { get; set; } = ""; // "Countdown", "Process Monitor", "Idle Detection", "Schedule", "Quick Action"
    public string Details { get; set; } = "";
    public bool WasCancelled { get; set; }
    public string StatusText => WasCancelled ? "Cancelled" : "Executed";
    public string FriendlyTimestamp => Timestamp.ToString("MMM d, HH:mm");
}
