using ShutdownTimer.Services;

namespace ShutdownTimer.Tests;

[TestClass]
public class SettingsIntegrityTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ShutdownTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup on CI agents.
        }
    }

    [TestMethod]
    public void Integrity_RoundTrip_Succeeds()
    {
        const string json = """{"settingsVersion":4,"minimizeToTrayOnClose":true}""";

        SettingsIntegrity.WriteIntegrity(_tempDir, json);

        Assert.IsTrue(SettingsIntegrity.TryVerify(_tempDir, json, out var reason));
        Assert.IsNull(reason);
    }

    [TestMethod]
    public void Integrity_TamperedJson_FailsVerification()
    {
        const string json = """{"settingsVersion":4}""";
        SettingsIntegrity.WriteIntegrity(_tempDir, json);

        const string tampered = """{"settingsVersion":4,"runAtStartup":true}""";

        Assert.IsFalse(SettingsIntegrity.TryVerify(_tempDir, tampered, out var reason));
        Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
    }

    [TestMethod]
    public void Integrity_MissingFile_AllowsFirstRunMigration()
    {
        const string json = """{"settingsVersion":4}""";

        Assert.IsTrue(SettingsIntegrity.TryVerify(_tempDir, json, out var reason));
        Assert.IsNull(reason);
    }
}
