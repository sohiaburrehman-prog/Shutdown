using System.Text.Json;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
}

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShutdownTimer");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings Settings { get; private set; } = new();

    private const int CurrentVersion = 4;

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = await File.ReadAllTextAsync(SettingsFile);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

                // Migrate from older schema versions
                if (Settings.SettingsVersion < CurrentVersion)
                {
                    MigrateSettings(Settings);
                    Settings.SettingsVersion = CurrentVersion;
                    await SaveAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            Settings = new AppSettings();
        }
    }

    private static void MigrateSettings(AppSettings settings)
    {
        if (settings.SettingsVersion < 2)
        {
            // v2 added: QuickPresets, PreActionProgramPath, History, ShowToastNotification,
            // ScheduleEntry.IsOneTime/OneTimeTarget, ProcessInfo.MemoryMB/CpuTime
            settings.QuickPresets ??= new()
            {
                new() { Label = "15 min", TotalMinutes = 15 },
                new() { Label = "30 min", TotalMinutes = 30 },
                new() { Label = "1 hour", TotalMinutes = 60 },
                new() { Label = "2 hours", TotalMinutes = 120 },
            };
            settings.History ??= new();
        }
        if (settings.SettingsVersion < 4)
        {
            settings.LowBatteryThreshold = settings.LowBatteryThreshold <= 0 ? 20 : settings.LowBatteryThreshold;
            settings.CriticalBatteryThreshold = settings.CriticalBatteryThreshold <= 0 ? 10 : settings.CriticalBatteryThreshold;
            settings.LowBatteryAction = settings.LowBatteryAction;
            settings.CriticalBatteryAction = settings.CriticalBatteryAction;
            settings.BatteryAutomationOnlyWhenUnplugged = true;
        }
        System.Diagnostics.Debug.WriteLine(
            $"[SettingsService] Migrated settings from v{settings.SettingsVersion} to v{CurrentVersion}");
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
        }
    }
}
