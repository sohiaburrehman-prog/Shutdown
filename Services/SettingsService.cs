using System.Text;
using System.Text.Json;
using ShutdownTimer.Helpers;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    bool SettingsWereTampered { get; }
    string? IntegrityMessage { get; }
    Task LoadAsync();
    Task SaveAsync();
    Task ClearAllDataAsync();
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
    public bool SettingsWereTampered { get; private set; }
    public string? IntegrityMessage { get; private set; }

    private const int CurrentVersion = 4;

    public async Task LoadAsync()
    {
        SettingsWereTampered = false;
        IntegrityMessage = null;

        try
        {
            if (!File.Exists(SettingsFile))
            {
                Settings = new AppSettings();
                StartupRegistryHelper.MigrateLegacyEntry(GetExecutablePath());
                return;
            }

            var fileInfo = new FileInfo(SettingsFile);
            if (fileInfo.Length > SettingsIntegrity.MaxJsonBytes)
            {
                ResetAfterIntegrityFailure("Settings file exceeds the maximum allowed size.");
                return;
            }

            var json = await File.ReadAllTextAsync(SettingsFile);
            if (Encoding.UTF8.GetByteCount(json) > SettingsIntegrity.MaxJsonBytes)
            {
                ResetAfterIntegrityFailure("Settings file exceeds the maximum allowed size.");
                return;
            }

            if (!SettingsIntegrity.TryVerify(SettingsDir, json, out var failureReason))
            {
                ResetAfterIntegrityFailure(failureReason ?? "Settings integrity check failed.");
                return;
            }

            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            if (Settings.SettingsVersion < CurrentVersion)
            {
                MigrateSettings(Settings);
                Settings.SettingsVersion = CurrentVersion;
                await SaveAsync();
            }

            StartupRegistryHelper.MigrateLegacyEntry(GetExecutablePath());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            Settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFile, json);
            SettingsIntegrity.WriteIntegrity(SettingsDir, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
        }
    }

    public async Task ClearAllDataAsync()
    {
        Settings = new AppSettings();
        SettingsWereTampered = false;
        IntegrityMessage = null;

        StartupRegistryHelper.RemoveAll();

        try
        {
            if (File.Exists(SettingsFile))
                File.Delete(SettingsFile);
            SettingsIntegrity.DeleteIntegrity(SettingsDir);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to delete settings files: {ex.Message}");
        }

        await SaveAsync();
    }

    private void ResetAfterIntegrityFailure(string reason)
    {
        System.Diagnostics.Debug.WriteLine($"[SettingsService] {reason} Resetting to defaults.");
        SettingsWereTampered = true;
        IntegrityMessage = reason;
        Settings = new AppSettings();
        _ = SaveAsync();
    }

    private static void MigrateSettings(AppSettings settings)
    {
        if (settings.SettingsVersion < 2)
        {
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
            settings.BatteryAutomationOnlyWhenUnplugged = true;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[SettingsService] Migrated settings from v{settings.SettingsVersion} to v{CurrentVersion}");
    }

    private static string? GetExecutablePath() =>
        Environment.ProcessPath;
}
