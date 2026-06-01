using Microsoft.Win32;

namespace ShutdownTimer.Helpers;

/// <summary>
/// Manages the Run-at-startup registry entry with a single canonical value name.
/// Removes legacy installer key names on read/write.
/// </summary>
public static class StartupRegistryHelper
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "ShutdownTimerAdvanced";

    /// <summary>Legacy value written by older installers.</summary>
    public static readonly string[] LegacyValueNames = ["ShutdownTimer"];

    public static void SetRunAtStartup(bool enabled, string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            return;

        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null)
            return;

        RemoveLegacyEntries(key);

        if (enabled)
            key.SetValue(ValueName, $"\"{exePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }

    public static void RemoveAll()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null)
            return;

        key.DeleteValue(ValueName, false);
        RemoveLegacyEntries(key);
    }

    /// <summary>
    /// If only the legacy key exists, rename it to the canonical value name.
    /// </summary>
    public static void MigrateLegacyEntry(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            return;

        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        if (key == null)
            return;

        var current = key.GetValue(ValueName) as string;
        if (!string.IsNullOrWhiteSpace(current))
        {
            RemoveLegacyEntries(key);
            return;
        }

        foreach (var legacy in LegacyValueNames)
        {
            var legacyValue = key.GetValue(legacy) as string;
            if (string.IsNullOrWhiteSpace(legacyValue))
                continue;

            key.SetValue(ValueName, legacyValue);
            key.DeleteValue(legacy, false);
            return;
        }
    }

    private static void RemoveLegacyEntries(RegistryKey key)
    {
        foreach (var legacy in LegacyValueNames)
            key.DeleteValue(legacy, false);
    }
}
