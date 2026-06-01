using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class CountdownTimerViewModel : ObservableObject
{
    private readonly ITimerService _timerService;
    private readonly ISystemActionService _actionService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private int hours;
    [ObservableProperty] private int minutes = 30;
    [ObservableProperty] private int seconds;
    [ObservableProperty] private string remainingDisplay = "00:00:00";
    [ObservableProperty] private double progressPercent;
    [ObservableProperty] private TimerAction selectedAction = TimerAction.Shutdown;
    [ObservableProperty] private TimerState state = TimerState.Idle;
    [ObservableProperty] private bool canStart = true;
    [ObservableProperty] private bool canPause;
    [ObservableProperty] private bool canResume;
    [ObservableProperty] private bool canCancel;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private ObservableCollection<PresetEntry> quickPresets = new();
    [ObservableProperty] private string validationMessage = "";
    [ObservableProperty] private string durationSummary = "30 minutes";
    [ObservableProperty] private string actionPreview = "Shutdown this PC after 30 minutes.";
    [ObservableProperty] private string runningSummary = "Timer is ready.";

    private TimeSpan _totalDuration;

    public TimerAction[] AvailableActions { get; } = Enum.GetValues<TimerAction>();

    public CountdownTimerViewModel(
        ITimerService timerService,
        ISystemActionService actionService,
        ISettingsService settingsService)
    {
        _timerService = timerService;
        _actionService = actionService;
        _settingsService = settingsService;

        _timerService.Tick += OnTick;
        _timerService.Completed += OnCompleted;

        LoadPresets();
        UpdatePreview();
    }

    public void LoadPresets()
    {
        QuickPresets = new ObservableCollection<PresetEntry>(_settingsService.Settings.QuickPresets);
    }

    /// <summary>
    /// Starts a countdown from taskbar jump list or other quick-launch entry points.
    /// </summary>
    public void StartQuickCountdown(int totalMinutes)
    {
        if (totalMinutes <= 0)
            return;

        Hours = totalMinutes / 60;
        Minutes = totalMinutes % 60;
        Seconds = 0;
        SelectedAction = _settingsService.Settings.DefaultAction;
        UpdatePreview();

        _totalDuration = new TimeSpan(Hours, Minutes, Seconds);
        ValidationMessage = "";
        _timerService.Start(_totalDuration);
        UpdateState(TimerState.Running);
        RunningSummary = $"{SelectedAction} will run after {DurationSummary.ToLowerInvariant()}.";
    }

    [RelayCommand]
    private void SetPreset(PresetEntry? preset)
    {
        if (preset == null) return;
        Hours = preset.TotalMinutes / 60;
        Minutes = preset.TotalMinutes % 60;
        Seconds = 0;
        UpdatePreview();
    }

    [RelayCommand]
    private void Set4HourPreset()
    {
        Hours = 4;
        Minutes = 0;
        Seconds = 0;
        UpdatePreview();
    }

    [RelayCommand]
    private void UseMovieNightTemplate()
    {
        Hours = 2;
        Minutes = 0;
        Seconds = 0;
        SelectedAction = TimerAction.Sleep;
        UpdatePreview();
    }

    [RelayCommand]
    private void UseUpdateRestartTemplate()
    {
        Hours = 0;
        Minutes = 30;
        Seconds = 0;
        SelectedAction = TimerAction.Restart;
        UpdatePreview();
    }

    [RelayCommand]
    private void UseLongDownloadTemplate()
    {
        Hours = 4;
        Minutes = 0;
        Seconds = 0;
        SelectedAction = TimerAction.Hibernate;
        UpdatePreview();
    }

    partial void OnHoursChanged(int value) => UpdatePreview();
    partial void OnMinutesChanged(int value) => UpdatePreview();
    partial void OnSecondsChanged(int value) => UpdatePreview();
    partial void OnSelectedActionChanged(TimerAction value) => UpdatePreview();

    private void UpdatePreview()
    {
        var duration = new TimeSpan(Math.Max(0, Hours), Math.Max(0, Minutes), Math.Max(0, Seconds));
        DurationSummary = FormatDuration(duration);
        ActionPreview = duration > TimeSpan.Zero
            ? $"{SelectedAction} this PC after {DurationSummary.ToLowerInvariant()}."
            : "Choose a duration to preview the automation.";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return "No duration set";

        var parts = new List<string>();
        if (duration.Hours > 0) parts.Add($"{duration.Hours} hour{(duration.Hours == 1 ? "" : "s")}");
        if (duration.Minutes > 0) parts.Add($"{duration.Minutes} minute{(duration.Minutes == 1 ? "" : "s")}");
        if (duration.Seconds > 0) parts.Add($"{duration.Seconds} second{(duration.Seconds == 1 ? "" : "s")}");

        return string.Join(", ", parts);
    }

    [RelayCommand]
    private void Start()
    {
        _totalDuration = new TimeSpan(Hours, Minutes, Seconds);
        if (_totalDuration <= TimeSpan.Zero)
        {
            ValidationMessage = "Please set a duration greater than zero before starting.";
            return;
        }

        ValidationMessage = "";
        _timerService.Start(_totalDuration);
        UpdateState(TimerState.Running);
        RunningSummary = $"{SelectedAction} will run after {DurationSummary.ToLowerInvariant()}.";
    }

    [RelayCommand]
    private void Pause()
    {
        _timerService.Pause();
        UpdateState(TimerState.Paused);
        RunningSummary = "Timer paused. Resume when you are ready.";
    }

    [RelayCommand]
    private void Resume()
    {
        _timerService.Resume();
        UpdateState(TimerState.Running);
        RunningSummary = $"{SelectedAction} will run after the remaining time.";
    }

    [RelayCommand]
    private void Cancel()
    {
        _timerService.Cancel();
        UpdateState(TimerState.Idle);
        RemainingDisplay = "00:00:00";
        ProgressPercent = 0;
        RunningSummary = "Timer cancelled.";
    }

    private void UpdateState(TimerState newState)
    {
        State = newState;
        IsRunning = newState == TimerState.Running;
        CanStart = newState == TimerState.Idle || newState == TimerState.Cancelled;
        CanPause = newState == TimerState.Running;
        CanResume = newState == TimerState.Paused;
        CanCancel = newState == TimerState.Running || newState == TimerState.Paused;
    }

    private void OnTick(TimeSpan remaining)
    {
        RemainingDisplay = remaining.ToString(@"hh\:mm\:ss");
        RunningSummary = $"{SelectedAction} in {remaining:hh\\:mm\\:ss}.";
        ProgressPercent = _totalDuration.TotalSeconds > 0
            ? (1.0 - remaining.TotalSeconds / _totalDuration.TotalSeconds) * 100
            : 0;
    }

    private void OnCompleted()
    {
        UpdateState(TimerState.Completed);
        RemainingDisplay = "00:00:00";
        ProgressPercent = 100;
        _actionService.Execute(SelectedAction, "Countdown", $"Duration: {_totalDuration:hh\\:mm\\:ss}");
    }
}
