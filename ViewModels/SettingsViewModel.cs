using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Helpers;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private bool _isLoading;

    [ObservableProperty] private bool minimizeToTrayOnClose;
    [ObservableProperty] private bool startMinimized;
    [ObservableProperty] private bool showWarningNotification;
    [ObservableProperty] private bool showToastNotification;
    [ObservableProperty] private int warningSeconds = 30;
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private bool runAtStartup;
    [ObservableProperty] private bool enableNotificationSound;
    [ObservableProperty] private TimerAction defaultAction = TimerAction.Shutdown;
    [ObservableProperty] private bool lowBatteryAutomationEnabled;
    [ObservableProperty] private int lowBatteryThreshold = 20;
    [ObservableProperty] private TimerAction lowBatteryAction = TimerAction.Sleep;
    [ObservableProperty] private bool criticalBatteryAutomationEnabled;
    [ObservableProperty] private int criticalBatteryThreshold = 10;
    [ObservableProperty] private TimerAction criticalBatteryAction = TimerAction.Hibernate;
    [ObservableProperty] private bool batteryAutomationOnlyWhenUnplugged = true;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isStatusError;
    [ObservableProperty] private string preActionProgramPath = "";
    [ObservableProperty] private int preActionTimeoutSeconds = 30;
    [ObservableProperty] private ObservableCollection<PresetEntry> quickPresets = new();

    // New preset fields
    [ObservableProperty] private string newPresetLabel = "";
    [ObservableProperty] private int newPresetMinutes = 15;

    public TimerAction[] AvailableActions { get; } = Enum.GetValues<TimerAction>();
    public string[] AvailableThemes { get; } = ["System (follow Windows)", "Light", "Dark"];

    public event Action<int>? ThemeChanged;
    public event Action? AllDataCleared;

    partial void OnThemeIndexChanged(int value)
    {
        ThemeChanged?.Invoke(value);
        AutoSave();
    }

    partial void OnMinimizeToTrayOnCloseChanged(bool value) => AutoSave();
    partial void OnStartMinimizedChanged(bool value) => AutoSave();
    partial void OnShowWarningNotificationChanged(bool value) => AutoSave();
    partial void OnShowToastNotificationChanged(bool value) => AutoSave();
    partial void OnWarningSecondsChanged(int value) => AutoSave();
    partial void OnDefaultActionChanged(TimerAction value) => AutoSave();
    partial void OnPreActionProgramPathChanged(string value) => AutoSave();
    partial void OnPreActionTimeoutSecondsChanged(int value) => AutoSave();
    partial void OnEnableNotificationSoundChanged(bool value) => AutoSave();
    partial void OnLowBatteryAutomationEnabledChanged(bool value) => AutoSave();
    partial void OnLowBatteryThresholdChanged(int value) => AutoSave();
    partial void OnLowBatteryActionChanged(TimerAction value) => AutoSave();
    partial void OnCriticalBatteryAutomationEnabledChanged(bool value) => AutoSave();
    partial void OnCriticalBatteryThresholdChanged(int value) => AutoSave();
    partial void OnCriticalBatteryActionChanged(TimerAction value) => AutoSave();
    partial void OnBatteryAutomationOnlyWhenUnpluggedChanged(bool value) => AutoSave();

    partial void OnRunAtStartupChanged(bool value)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null) return;

            StartupRegistryHelper.SetRunAtStartup(value, exePath);
            AutoSave();
            ShowStatus($"Run at startup {(value ? "enabled" : "disabled")}.");
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to update startup setting: {ex.Message}", true);
        }
    }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Load()
    {
        _isLoading = true;
        var s = _settingsService.Settings;
        MinimizeToTrayOnClose = s.MinimizeToTrayOnClose;
        StartMinimized = s.StartMinimized;
        ShowWarningNotification = s.ShowWarningNotification;
        ShowToastNotification = s.ShowToastNotification;
        WarningSeconds = s.WarningSeconds;
        DefaultAction = s.DefaultAction;
        ThemeIndex = s.ThemeIndex;
        RunAtStartup = s.RunAtStartup;
        EnableNotificationSound = s.EnableNotificationSound;
        LowBatteryAutomationEnabled = s.LowBatteryAutomationEnabled;
        LowBatteryThreshold = s.LowBatteryThreshold;
        LowBatteryAction = s.LowBatteryAction;
        CriticalBatteryAutomationEnabled = s.CriticalBatteryAutomationEnabled;
        CriticalBatteryThreshold = s.CriticalBatteryThreshold;
        CriticalBatteryAction = s.CriticalBatteryAction;
        BatteryAutomationOnlyWhenUnplugged = s.BatteryAutomationOnlyWhenUnplugged;
        PreActionProgramPath = s.PreActionProgramPath ?? "";
        PreActionTimeoutSeconds = s.PreActionTimeoutSeconds;
        QuickPresets = new ObservableCollection<PresetEntry>(s.QuickPresets);
        _isLoading = false;

        if (_settingsService.SettingsWereTampered)
        {
            ShowStatus(
                _settingsService.IntegrityMessage ??
                "Settings were reset because the saved configuration failed integrity verification.",
                true);
        }
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusText = message;
        IsStatusError = isError;
        // Reset after 3 seconds
        _ = Task.Delay(3000).ContinueWith(_ => 
        {
            if (StatusText == message) StatusText = "";
        });
    }

    private void AutoSave()
    {
        if (_isLoading) return;

        var s = _settingsService.Settings;
        s.MinimizeToTrayOnClose = MinimizeToTrayOnClose;
        s.StartMinimized = StartMinimized;
        s.ShowWarningNotification = ShowWarningNotification;
        s.ShowToastNotification = ShowToastNotification;
        s.WarningSeconds = WarningSeconds;
        s.DefaultAction = DefaultAction;
        s.ThemeIndex = ThemeIndex;
        s.RunAtStartup = RunAtStartup;
        s.EnableNotificationSound = EnableNotificationSound;
        s.LowBatteryAutomationEnabled = LowBatteryAutomationEnabled;
        s.LowBatteryThreshold = Math.Clamp(LowBatteryThreshold, 5, 95);
        s.LowBatteryAction = LowBatteryAction;
        s.CriticalBatteryAutomationEnabled = CriticalBatteryAutomationEnabled;
        s.CriticalBatteryThreshold = Math.Clamp(CriticalBatteryThreshold, 1, 90);
        s.CriticalBatteryAction = CriticalBatteryAction;
        s.BatteryAutomationOnlyWhenUnplugged = BatteryAutomationOnlyWhenUnplugged;
        s.PreActionProgramPath = string.IsNullOrWhiteSpace(PreActionProgramPath) ? null : PreActionProgramPath;
        s.PreActionTimeoutSeconds = PreActionTimeoutSeconds;

        _ = _settingsService.SaveAsync();
        ShowStatus("Settings saved.");
    }

    [RelayCommand]
    private void TestPreActionPath()
    {
        if (string.IsNullOrWhiteSpace(PreActionProgramPath))
        {
            ShowStatus("Enter a path to test first.", true);
            return;
        }

        var path = PreActionProgramPath.Trim();
        if (!File.Exists(path))
        {
            ShowStatus($"File not found: {path}", true);
            return;
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".exe" or ".com"))
        {
            ShowStatus("Only .exe and .com programs are supported for pre-action execution.", true);
            return;
        }

        ShowStatus($"Path exists and extension is allowed: {ext}");
    }

    [RelayCommand]
    private async Task AddPreset()
    {
        if (string.IsNullOrWhiteSpace(NewPresetLabel) || NewPresetMinutes <= 0)
        {
            StatusText = "Enter a label and positive minutes.";
            return;
        }

        var preset = new PresetEntry { Label = NewPresetLabel.Trim(), TotalMinutes = NewPresetMinutes };
        QuickPresets.Add(preset);
        _settingsService.Settings.QuickPresets.Add(preset);
        await _settingsService.SaveAsync();

        NewPresetLabel = "";
        NewPresetMinutes = 15;
        StatusText = $"Added preset: {preset.Label}";
    }

    [RelayCommand]
    private async Task RemovePreset(PresetEntry? preset)
    {
        if (preset is null) return;

        QuickPresets.Remove(preset);
        _settingsService.Settings.QuickPresets.Remove(preset);
        await _settingsService.SaveAsync();
        StatusText = $"Removed preset: {preset.Label}";
    }

    [RelayCommand]
    private async Task ClearAllData()
    {
        await _settingsService.ClearAllDataAsync();
        Load();
        ThemeChanged?.Invoke(ThemeIndex);
        AllDataCleared?.Invoke();
        StatusText = "All local data cleared. Settings, schedules, history, and startup entry were reset.";
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new AppSettings();
        _isLoading = true;
        MinimizeToTrayOnClose = defaults.MinimizeToTrayOnClose;
        StartMinimized = defaults.StartMinimized;
        ShowWarningNotification = defaults.ShowWarningNotification;
        ShowToastNotification = defaults.ShowToastNotification;
        WarningSeconds = defaults.WarningSeconds;
        DefaultAction = defaults.DefaultAction;
        ThemeIndex = defaults.ThemeIndex;
        LowBatteryAutomationEnabled = defaults.LowBatteryAutomationEnabled;
        LowBatteryThreshold = defaults.LowBatteryThreshold;
        LowBatteryAction = defaults.LowBatteryAction;
        CriticalBatteryAutomationEnabled = defaults.CriticalBatteryAutomationEnabled;
        CriticalBatteryThreshold = defaults.CriticalBatteryThreshold;
        CriticalBatteryAction = defaults.CriticalBatteryAction;
        BatteryAutomationOnlyWhenUnplugged = defaults.BatteryAutomationOnlyWhenUnplugged;
        PreActionProgramPath = "";
        PreActionTimeoutSeconds = defaults.PreActionTimeoutSeconds;
        QuickPresets = new ObservableCollection<PresetEntry>(defaults.QuickPresets);
        _settingsService.Settings.QuickPresets = new List<PresetEntry>(defaults.QuickPresets);
        _isLoading = false;

        AutoSave();
        ThemeChanged?.Invoke(ThemeIndex);
        StatusText = "Settings reset to defaults.";
    }
}
