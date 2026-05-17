using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class ProcessMonitorViewModel : ObservableObject
{
    private readonly IProcessMonitorService _monitorService;
    private readonly ISystemActionService _actionService;

    [ObservableProperty] private ObservableCollection<ProcessInfo> processes = new();
    [ObservableProperty] private ProcessInfo? selectedProcess;
    [ObservableProperty] private TimerAction selectedAction = TimerAction.Shutdown;
    [ObservableProperty] private bool isMonitoring;
    [ObservableProperty] private string statusText = "Select a process to monitor.";
    [ObservableProperty] private string elapsedText = "";
    [ObservableProperty] private string filterText = "";
    [ObservableProperty] private bool isProcessListEmpty = true;
    [ObservableProperty] private int monitorModeIndex; // 0 = Wait for exit, 1 = Wait for start
    [ObservableProperty] private string waitForProcessName = "";
    [ObservableProperty] private string monitorPreview = "Select a running process, then choose what happens when it exits.";

    private List<ProcessInfo> _allProcesses = new();

    public TimerAction[] AvailableActions { get; } = Enum.GetValues<TimerAction>();
    public string[] MonitorModes { get; } = ["When process exits", "When process starts"];

    public ProcessMonitorViewModel(
        IProcessMonitorService monitorService,
        ISystemActionService actionService)
    {
        _monitorService = monitorService;
        _actionService = actionService;

        _monitorService.TargetProcessExited += OnProcessExited;
        _monitorService.TargetProcessStarted += OnProcessStarted;
        _monitorService.MonitorTick += OnMonitorTick;
    }

    [RelayCommand]
    private void RefreshProcesses()
    {
        _allProcesses = _monitorService.GetRunningProcesses();
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnMonitorModeIndexChanged(int value) => UpdatePreview();
    partial void OnWaitForProcessNameChanged(string value) => UpdatePreview();
    partial void OnSelectedActionChanged(TimerAction value) => UpdatePreview();
    partial void OnSelectedProcessChanged(ProcessInfo? value) => UpdatePreview();

    private void UpdatePreview()
    {
        if (MonitorModeIndex == 1)
        {
            var name = string.IsNullOrWhiteSpace(WaitForProcessName) ? "the named process" : WaitForProcessName.Trim();
            MonitorPreview = $"{SelectedAction} when {name} starts.";
            return;
        }

        MonitorPreview = SelectedProcess is null
            ? $"Select a running process, then {SelectedAction} when it exits."
            : $"{SelectedAction} when {SelectedProcess.DisplayName} exits.";
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(FilterText)
            ? _allProcesses
            : _allProcesses.Where(p =>
                p.ProcessName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                (p.MainWindowTitle?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        Processes = new ObservableCollection<ProcessInfo>(filtered);
        IsProcessListEmpty = Processes.Count == 0;
    }

    [RelayCommand]
    private void StartMonitoring()
    {
        if (MonitorModeIndex == 1)
        {
            // Wait for process start mode
            if (string.IsNullOrWhiteSpace(WaitForProcessName))
            {
                StatusText = "Enter a process name to watch for.";
                return;
            }
            _monitorService.StartWaitForProcess(WaitForProcessName.Trim());
            IsMonitoring = true;
            StatusText = $"Waiting for \"{WaitForProcessName.Trim()}\" to start...";
            return;
        }

        // Wait for exit mode
        if (SelectedProcess is null)
        {
            StatusText = "Select a process first.";
            return;
        }

        _monitorService.StartMonitoring(SelectedProcess);
        IsMonitoring = true;
        StatusText = $"Monitoring: {SelectedProcess.DisplayName} (PID {SelectedProcess.ProcessId})";
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _monitorService.StopMonitoring();
        IsMonitoring = false;
        StatusText = "Monitoring stopped.";
        ElapsedText = "";
    }

    private void OnProcessExited()
    {
        var processName = _monitorService.MonitoredProcess?.DisplayName ?? "Unknown";
        IsMonitoring = false;
        StatusText = "Process has exited. Executing action...";
        _actionService.Execute(SelectedAction, "Process Monitor", $"Process exited: {processName}");
    }

    private void OnProcessStarted()
    {
        var processName = _monitorService.MonitoredProcess?.DisplayName ?? "Unknown";
        IsMonitoring = false;
        StatusText = $"Process \"{processName}\" detected! Executing action...";
        _actionService.Execute(SelectedAction, "Process Monitor", $"Process started: {processName}");
    }

    private void OnMonitorTick(TimeSpan elapsed)
    {
        ElapsedText = $"Monitoring for: {elapsed:hh\\:mm\\:ss}";
    }
}
