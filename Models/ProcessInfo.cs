namespace ShutdownTimer.Models;

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? MainWindowTitle { get; set; }
    public string? ExecutablePath { get; set; }

    /// <summary>Memory usage in MB.</summary>
    public double MemoryMB { get; set; }

    /// <summary>CPU time consumed (user + kernel) at snapshot time.</summary>
    public string CpuTime { get; set; } = "";

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(MainWindowTitle)
            ? $"{MainWindowTitle} ({ProcessName})"
            : ProcessName;

    public string ResourceDisplay =>
        $"{MemoryMB:F0} MB";

    public override string ToString() => DisplayName;
}
