using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public enum ProcessMonitorMode
{
    WaitForExit,
    WaitForStart
}

public interface IProcessMonitorService
{
    event Action? TargetProcessExited;
    event Action? TargetProcessStarted;
    event Action<TimeSpan>? MonitorTick;

    bool IsMonitoring { get; }
    ProcessInfo? MonitoredProcess { get; }
    ProcessMonitorMode MonitorMode { get; }

    List<ProcessInfo> GetRunningProcesses();
    void StartMonitoring(ProcessInfo process, ProcessMonitorMode mode = ProcessMonitorMode.WaitForExit);
    void StartWaitForProcess(string processName, ProcessMonitorMode mode = ProcessMonitorMode.WaitForStart);
    void StopMonitoring();
}

public class ProcessMonitorService : IProcessMonitorService
{
    private CancellationTokenSource? _cts;
    private readonly DispatcherQueue _dispatcher;

    public event Action? TargetProcessExited;
    public event Action? TargetProcessStarted;
    public event Action<TimeSpan>? MonitorTick;

    public bool IsMonitoring { get; private set; }
    public ProcessInfo? MonitoredProcess { get; private set; }
    public ProcessMonitorMode MonitorMode { get; private set; }

    public ProcessMonitorService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public List<ProcessInfo> GetRunningProcesses()
    {
        return Process.GetProcesses()
            .Where(p =>
            {
                try { return !string.IsNullOrWhiteSpace(p.MainWindowTitle); }
                catch { return false; }
            })
            .Select(p =>
            {
                try
                {
                    double memMb = 0;
                    string cpuTime = "";
                    try
                    {
                        memMb = p.WorkingSet64 / (1024.0 * 1024.0);
                        cpuTime = p.TotalProcessorTime.ToString(@"hh\:mm\:ss");
                    }
                    catch { }

                    return new ProcessInfo
                    {
                        ProcessId = p.Id,
                        ProcessName = p.ProcessName,
                        MainWindowTitle = p.MainWindowTitle,
                        ExecutablePath = TryGetPath(p),
                        MemoryMB = memMb,
                        CpuTime = cpuTime
                    };
                }
                catch
                {
                    return new ProcessInfo
                    {
                        ProcessId = p.Id,
                        ProcessName = p.ProcessName
                    };
                }
            })
            .OrderBy(p => p.ProcessName)
            .ToList();
    }

    public void StartMonitoring(ProcessInfo process, ProcessMonitorMode mode = ProcessMonitorMode.WaitForExit)
    {
        StopMonitoring();

        MonitoredProcess = process;
        MonitorMode = mode;
        IsMonitoring = true;
        _cts = new CancellationTokenSource();

        _ = MonitorExitAsync(process.ProcessId, process.ProcessName, _cts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine($"[ProcessMonitorService] MonitorExit failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public void StartWaitForProcess(string processName, ProcessMonitorMode mode = ProcessMonitorMode.WaitForStart)
    {
        StopMonitoring();

        MonitoredProcess = new ProcessInfo { ProcessName = processName };
        MonitorMode = mode;
        IsMonitoring = true;
        _cts = new CancellationTokenSource();

        _ = MonitorStartAsync(processName, _cts.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine($"[ProcessMonitorService] MonitorStart failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        IsMonitoring = false;
        MonitoredProcess = null;
    }

    private async Task MonitorExitAsync(int pid, string expectedName, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(500, ct);

                bool exited;
                try
                {
                    var proc = Process.GetProcessById(pid);
                    exited = !string.Equals(proc.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase)
                             || proc.HasExited;
                }
                catch (ArgumentException)
                {
                    exited = true;
                }
                catch (UnauthorizedAccessException)
                {
                    exited = false;
                }

                var elapsed = DateTime.UtcNow - startTime;
                _dispatcher.TryEnqueue(() => 
                {
                    try { MonitorTick?.Invoke(elapsed); }
                    catch (Exception ex) { Debug.WriteLine($"[ProcessMonitorService] MonitorTick dispatch failed: {ex.Message}"); }
                });

                if (exited)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        try
                        {
                            IsMonitoring = false;
                            TargetProcessExited?.Invoke();
                        }
                        catch (Exception ex) { Debug.WriteLine($"[ProcessMonitorService] TargetProcessExited dispatch failed: {ex.Message}"); }
                    });
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task MonitorStartAsync(string processName, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);

                var elapsed = DateTime.UtcNow - startTime;
                _dispatcher.TryEnqueue(() =>
                {
                    try { MonitorTick?.Invoke(elapsed); }
                    catch (Exception ex) { Debug.WriteLine($"[ProcessMonitorService] MonitorTick dispatch failed: {ex.Message}"); }
                });

                // Check if a process with this name now exists
                var found = Process.GetProcessesByName(processName);
                if (found.Length > 0)
                {
                    var info = found[0];
                    var foundProcess = new ProcessInfo
                    {
                        ProcessId = info.Id,
                        ProcessName = info.ProcessName,
                        MainWindowTitle = TryGetTitle(info)
                    };

                    _dispatcher.TryEnqueue(() =>
                    {
                        try
                        {
                            MonitoredProcess = foundProcess;
                            IsMonitoring = false;
                            TargetProcessStarted?.Invoke();
                        }
                        catch (Exception ex) { Debug.WriteLine($"[ProcessMonitorService] TargetProcessStarted dispatch failed: {ex.Message}"); }
                    });
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static string? TryGetPath(Process p)
    {
        try { return p.MainModule?.FileName; }
        catch { return null; }
    }

    private static string? TryGetTitle(Process p)
    {
        try { return p.MainWindowTitle; }
        catch { return null; }
    }
}
