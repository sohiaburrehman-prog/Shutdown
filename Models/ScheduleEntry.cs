using System.Text.Json.Serialization;

namespace ShutdownTimer.Models;

public class ScheduleEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CronExpression { get; set; } = "0 23 * * *";
    public TimerAction Action { get; set; } = TimerAction.Shutdown;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string? FriendlyDescription { get; set; }

    /// <summary>
    /// If true, this schedule fires once at the specified time and then auto-disables itself.
    /// </summary>
    public bool IsOneTime { get; set; }

    /// <summary>
    /// For one-time schedules: the exact DateTime to fire at.
    /// </summary>
    public DateTime? OneTimeTarget { get; set; }

    /// <summary>
    /// Runtime-only: set by ScheduleService when the cron expression fails to parse.
    /// Not persisted to settings.json.
    /// </summary>
    [JsonIgnore]
    public string? CronError { get; set; }

    [JsonIgnore]
    public string NextRunDisplay => NextRun.HasValue
        ? $"Next: {NextRun.Value:ddd MMM d, HH:mm}"
        : IsEnabled ? "Next run unavailable" : "Disabled";

    [JsonIgnore]
    public string LastRunDisplay => LastRun.HasValue
        ? $"Last: {LastRun.Value:MMM d, HH:mm}"
        : "Never run";

    [JsonIgnore]
    public string HealthText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CronError)) return CronError;
            if (!IsEnabled) return "Paused";
            if (NextRun.HasValue && NextRun.Value < DateTime.Now) return "Missed run - waiting for refresh";
            return "Ready";
        }
    }
}
