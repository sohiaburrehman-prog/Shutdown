using CommunityToolkit.Mvvm.ComponentModel;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITimerService _timerService;
    private readonly IProcessMonitorService _processService;
    private readonly IIdleDetectionService _idleService;
    private readonly IScheduleService _scheduleService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private bool isTimerActive;
    [ObservableProperty] private bool isProcessActive;
    [ObservableProperty] private bool isIdleActive;
    [ObservableProperty] private bool isScheduleActive;

    public MainViewModel(
        ITimerService timerService,
        IProcessMonitorService processService,
        IIdleDetectionService idleService,
        IScheduleService scheduleService,
        ISettingsService settingsService)
    {
        _timerService = timerService;
        _processService = processService;
        _idleService = idleService;
        _scheduleService = scheduleService;
        _settingsService = settingsService;

        // Poll for status updates (simple approach for global indicators)
        var timer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => UpdateStatus();
        timer.Start();

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        IsTimerActive = _timerService.State == TimerState.Running || _timerService.State == TimerState.Paused;
        IsProcessActive = _processService.IsMonitoring;
        IsIdleActive = _idleService.IsMonitoring;
        IsScheduleActive = _settingsService.Settings.Schedules.Any(s => s.IsEnabled);
    }
}
