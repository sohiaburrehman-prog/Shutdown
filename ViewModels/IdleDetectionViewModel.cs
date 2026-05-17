using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class IdleDetectionViewModel : ObservableObject
{
    private readonly IIdleDetectionService _idleService;
    private readonly ISystemActionService _actionService;

    [ObservableProperty] private int idleMinutes = 30;
    [ObservableProperty] private string currentIdleDisplay = "00:00";
    [ObservableProperty] private TimerAction selectedAction = TimerAction.Sleep;
    [ObservableProperty] private bool isMonitoring;
    [ObservableProperty] private string statusText = "Set the idle threshold and start monitoring.";
    [ObservableProperty] private double idleProgressPercent;
    [ObservableProperty] private string thresholdDisplay = "";
    [ObservableProperty] private string idlePreview = "Sleep this PC after 30 minutes with no keyboard or mouse input.";

    public TimerAction[] AvailableActions { get; } = Enum.GetValues<TimerAction>();

    public IdleDetectionViewModel(
        IIdleDetectionService idleService,
        ISystemActionService actionService)
    {
        _idleService = idleService;
        _actionService = actionService;

        _idleService.IdleTick += OnIdleTick;
        _idleService.IdleThresholdReached += OnThresholdReached;
        UpdatePreview();
    }

    partial void OnIdleMinutesChanged(int value) => UpdatePreview();
    partial void OnSelectedActionChanged(TimerAction value) => UpdatePreview();

    private void UpdatePreview()
    {
        IdlePreview = $"{SelectedAction} this PC after {IdleMinutes} minute{(IdleMinutes == 1 ? "" : "s")} with no keyboard or mouse input.";
    }

    [RelayCommand]
    private void StartMonitoring()
    {
        if (IdleMinutes <= 0) return;

        _idleService.StartMonitoring(IdleMinutes);
        IsMonitoring = true;
        StatusText = $"Monitoring idle time (threshold: {IdleMinutes} min)...";
    }

    [RelayCommand]
    private void UseDeskBreakTemplate()
    {
        IdleMinutes = 10;
        SelectedAction = TimerAction.Sleep;
    }

    [RelayCommand]
    private void UseBedtimeTemplate()
    {
        IdleMinutes = 15;
        SelectedAction = TimerAction.Shutdown;
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _idleService.StopMonitoring();
        IsMonitoring = false;
        StatusText = "Monitoring stopped.";
        CurrentIdleDisplay = "00:00";
        IdleProgressPercent = 0;
    }

    private void OnIdleTick(TimeSpan idle)
    {
        CurrentIdleDisplay = idle.ToString(@"mm\:ss");
        IdleProgressPercent = IdleMinutes > 0
            ? Math.Min(100, idle.TotalMinutes / IdleMinutes * 100)
            : 0;

        var threshold = TimeSpan.FromMinutes(IdleMinutes);
        ThresholdDisplay = $"{idle:mm\\:ss} / {threshold:mm\\:ss}";
    }

    private void OnThresholdReached()
    {
        IsMonitoring = false;
        StatusText = "Idle threshold reached. Executing action...";
        _actionService.Execute(SelectedAction, "Idle Detection", $"Idle threshold: {IdleMinutes} minutes");
    }
}
