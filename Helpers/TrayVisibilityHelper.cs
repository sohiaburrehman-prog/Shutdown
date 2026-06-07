using Microsoft.Win32;

namespace ShutdownTimer.Helpers;

/// <summary>
/// Windows 11 hides new tray icons until the user enables them under
/// Settings → Taskbar → Other system tray icons (NotifyIconSettings\IsPromoted).
/// </summary>
public static class TrayVisibilityHelper
{
    private const string KeyPath = @"Control Panel\NotifyIconSettings";
    private const string ExecutablePathValue = "ExecutablePath";
    private const string IsPromotedValue = "IsPromoted";

    public static async Task<bool> EnsurePromotedAsync(string executablePath, TimeSpan timeout)
    {
        var normalized = NormalizePath(executablePath);
        if (string.IsNullOrEmpty(normalized))
            return false;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (TryPromote(normalized))
                return true;

            await Task.Delay(500);
        }

        return TryPromote(normalized);
    }

    public static bool TryPromote(string executablePath)
    {
        var normalized = NormalizePath(executablePath);
        if (string.IsNullOrEmpty(normalized))
            return false;

        using var baseKey = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (baseKey == null)
            return false;

        var promoted = false;
        foreach (var subKeyName in baseKey.GetSubKeyNames())
        {
            using var subKey = baseKey.OpenSubKey(subKeyName, writable: true);
            if (subKey == null)
                continue;

            var exe = subKey.GetValue(ExecutablePathValue) as string;
            if (string.IsNullOrWhiteSpace(exe))
                continue;

            if (!PathsMatch(exe, normalized))
                continue;

            subKey.SetValue(IsPromotedValue, 1, RegistryValueKind.DWord);
            promoted = true;
        }

        return promoted;
    }

    public static bool HasRegistryEntry(string executablePath)
    {
        var normalized = NormalizePath(executablePath);
        if (string.IsNullOrEmpty(normalized))
            return false;

        using var baseKey = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (baseKey == null)
            return false;

        foreach (var subKeyName in baseKey.GetSubKeyNames())
        {
            using var subKey = baseKey.OpenSubKey(subKeyName);
            if (subKey == null)
                continue;

            var exe = subKey.GetValue(ExecutablePathValue) as string;
            if (!string.IsNullOrWhiteSpace(exe) && PathsMatch(exe, normalized))
                return true;
        }

        return false;
    }

    private static bool PathsMatch(string left, string right)
        => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return string.Empty;
        }
    }
}
