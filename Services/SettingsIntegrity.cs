using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace ShutdownTimer.Services;

/// <summary>
/// Detects tampering of settings.json using an HMAC keyed with a DPAPI-protected secret
/// stored alongside the settings file (per-user, per-machine).
/// </summary>
[SupportedOSPlatform("windows")]
public static class SettingsIntegrity
{
    public const int MaxJsonBytes = 1_048_576; // 1 MB

    private static readonly string KeyFileName = ".integrity-key";
    private static readonly string IntegrityFileName = "settings.integrity";

    public static string IntegrityFilePath(string settingsDir) =>
        Path.Combine(settingsDir, IntegrityFileName);

    public static string KeyFilePath(string settingsDir) =>
        Path.Combine(settingsDir, KeyFileName);

    public static bool TryVerify(string settingsDir, string json, out string? failureReason)
    {
        failureReason = null;
        var integrityPath = IntegrityFilePath(settingsDir);

        if (!File.Exists(integrityPath))
        {
            // First run after upgrade — no integrity file yet; caller will create one on save.
            return true;
        }

        if (!TryReadExpectedHash(settingsDir, out var expectedHash))
        {
            failureReason = "Integrity metadata is missing or unreadable.";
            return false;
        }

        var actualHash = ComputeHash(settingsDir, json);
        if (CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            return true;

        failureReason = "Settings file failed integrity verification.";
        return false;
    }

    public static void WriteIntegrity(string settingsDir, string json)
    {
        Directory.CreateDirectory(settingsDir);
        var hash = ComputeHash(settingsDir, json);
        File.WriteAllText(IntegrityFilePath(settingsDir), Convert.ToBase64String(hash));
    }

    public static void DeleteIntegrity(string settingsDir)
    {
        TryDelete(IntegrityFilePath(settingsDir));
        TryDelete(KeyFilePath(settingsDir));
    }

    private static byte[] ComputeHash(string settingsDir, string json)
    {
        var key = GetOrCreateKey(settingsDir);
        var payload = Encoding.UTF8.GetBytes(json);
        return HMACSHA256.HashData(key, payload);
    }

    private static bool TryReadExpectedHash(string settingsDir, out byte[] hash)
    {
        hash = Array.Empty<byte>();
        var path = IntegrityFilePath(settingsDir);
        if (!File.Exists(path))
            return false;

        try
        {
            var text = File.ReadAllText(path).Trim();
            hash = Convert.FromBase64String(text);
            return hash.Length == 32;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GetOrCreateKey(string settingsDir)
    {
        Directory.CreateDirectory(settingsDir);
        var keyPath = KeyFilePath(settingsDir);

        if (File.Exists(keyPath))
        {
            var protectedBytes = File.ReadAllBytes(keyPath);
            return ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        var protectedKey = ProtectedData.Protect(key, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, protectedKey);
        return key;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort during clear/reset.
        }
    }
}
