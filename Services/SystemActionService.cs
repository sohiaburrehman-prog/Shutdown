using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ShutdownTimer.Models;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ShutdownTimer.Services;

public interface ISystemActionService
{
    void Execute(TimerAction action, string trigger = "Unknown", string details = "");
    void ExecuteWithWarning(TimerAction action, string trigger = "Unknown", string details = "", int minimumWarningSeconds = 15);
    void CancelPendingShutdown();
    void CancelWarning();
    void PostponeWarning(int additionalSeconds);
    bool IsWarningActive { get; }
    event Action<int, TimerAction>? WarningTick;
    event Action? WarningCancelled;
}

public class SystemActionService : ISystemActionService
{
    private readonly ISettingsService _settingsService;
    private DispatcherTimer? _warningTimer;
    private int _warningRemaining;
    private TimerAction _pendingAction;
    private string _pendingTrigger = "";
    private string _pendingDetails = "";
    private DateTime _lastActionTime = DateTime.MinValue;
    private static readonly TimeSpan ActionCooldown = TimeSpan.FromSeconds(60);

    public bool IsWarningActive => _warningTimer != null;
    public event Action<int, TimerAction>? WarningTick;
    public event Action? WarningCancelled;
    private readonly ITaskbarService _taskbarService;

    public SystemActionService(ISettingsService settingsService, ITaskbarService taskbarService)
    {
        _settingsService = settingsService;
        _taskbarService = taskbarService;
    }

    public void Execute(TimerAction action, string trigger = "Unknown", string details = "")
    {
        try
        {
            // Rate limit: prevent rapid repeated execution (e.g. from buggy triggers)
            if (DateTime.UtcNow - _lastActionTime < ActionCooldown)
            {
                Debug.WriteLine($"[SystemActionService] Action cooldown active, ignoring {action} from {trigger}.");
                return;
            }

            var settings = _settingsService.Settings;
            if (settings.ShowWarningNotification && settings.WarningSeconds > 0)
            {
                _pendingTrigger = trigger;
                _pendingDetails = details;
                StartWarningCountdown(action, settings.WarningSeconds);

                // Send toast notification if enabled
                if (settings.ShowToastNotification)
                {
                    SendToastNotification(action, settings.WarningSeconds);
                }

                return;
            }

            RunPreActionAndExecute(action, trigger, details);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemActionService] Failed to execute {action}: {ex.Message}");
            throw;
        }
    }

    public void ExecuteWithWarning(TimerAction action, string trigger = "Unknown", string details = "", int minimumWarningSeconds = 15)
    {
        var settings = _settingsService.Settings;
        var previousShowWarning = settings.ShowWarningNotification;
        var previousWarningSeconds = settings.WarningSeconds;

        try
        {
            settings.ShowWarningNotification = true;
            settings.WarningSeconds = Math.Max(minimumWarningSeconds, settings.WarningSeconds);
            Execute(action, trigger, details);
        }
        finally
        {
            settings.ShowWarningNotification = previousShowWarning;
            settings.WarningSeconds = previousWarningSeconds;
        }
    }

    private void StartWarningCountdown(TimerAction action, int seconds)
    {
        CancelWarning();

        _pendingAction = action;
        _warningRemaining = seconds;

        // Play warning sound if enabled
        if (_settingsService.Settings.EnableNotificationSound)
        {
            try { Win32.NativeMethods.MessageBeep(0xFFFFFFFF); }
            catch { /* Ignore audio failures */ }
        }

        // Fire first tick immediately
        WarningTick?.Invoke(_warningRemaining, _pendingAction);

        _warningTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _warningTimer.Tick += WarningTimer_Tick;
        _warningTimer.Start();

        _taskbarService.SetState(Win32.NativeMethods.TBPFLAG.TBPF_PAUSED); // Yellow for warning
    }

    private void WarningTimer_Tick(object? sender, object e)
    {
        try
        {
            _warningRemaining--;

            if (_warningRemaining <= 0)
            {
                var action = _pendingAction;
                var trigger = _pendingTrigger;
                var details = _pendingDetails;
                CancelWarning();
                RunPreActionAndExecute(action, trigger, details);
            }
            else
            {
                WarningTick?.Invoke(_warningRemaining, _pendingAction);
                
                // Update taskbar progress (inverted since it's a countdown)
                var total = _settingsService.Settings.WarningSeconds;
                if (total > 0)
                {
                    var progress = (double)(total - _warningRemaining) / total;
                    _taskbarService.SetProgress(progress);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemActionService] Warning tick error: {ex.Message}");
            CancelWarning();
        }
    }

    public void PostponeWarning(int additionalSeconds)
    {
        if (_warningTimer == null) return;

        _warningRemaining += additionalSeconds;
        WarningTick?.Invoke(_warningRemaining, _pendingAction);
    }

    public void CancelWarning()
    {
        if (_warningTimer != null)
        {
            _warningTimer.Stop();
            _warningTimer.Tick -= WarningTimer_Tick;
            _warningTimer = null;

            // Log cancellation to history
            LogHistory(_pendingAction, _pendingTrigger, _pendingDetails, wasCancelled: true);

            WarningCancelled?.Invoke();
            _taskbarService.Reset();
        }
    }

    private void RunPreActionAndExecute(TimerAction action, string trigger, string details)
    {
        var settings = _settingsService.Settings;

        // Run pre-action program if configured
        if (!string.IsNullOrWhiteSpace(settings.PreActionProgramPath))
        {
            try
            {
                var path = settings.PreActionProgramPath.Trim();

                // Reject UNC paths — these could point to a remote share
                if (path.StartsWith(@"\\") || path.StartsWith("//"))
                {
                    Debug.WriteLine($"[SystemActionService] Pre-action program rejected: UNC/network paths are not allowed.");
                }
                else
                {
                    // Normalise path to prevent directory traversal (e.g. ..\..\evil.exe)
                    var fullPath = Path.GetFullPath(path);

                    if (!File.Exists(fullPath))
                    {
                        Debug.WriteLine($"[SystemActionService] Pre-action program not found: {fullPath}");
                    }
                    else
                    {
                        // Only allow known executable extensions
                        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
                        if (ext is not (".exe" or ".com"))
                        {
                            Debug.WriteLine($"[SystemActionService] Pre-action program has disallowed extension '{ext}', skipping.");
                        }
                        else
                        {
                            Debug.WriteLine($"[SystemActionService] Running pre-action program: {fullPath}");
                            var psi = new ProcessStartInfo(fullPath)
                            {
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            var proc = Process.Start(psi);
                            if (proc != null)
                            {
                                var timeoutMs = settings.PreActionTimeoutSeconds * 1000;
                                proc.WaitForExit(timeoutMs > 0 ? timeoutMs : 30000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemActionService] Pre-action program failed: {ex.Message}");
            }
        }

        // Log to history and record cooldown timestamp
        LogHistory(action, trigger, details, wasCancelled: false);
        _lastActionTime = DateTime.UtcNow;

        ExecuteImmediate(action);
    }

    private void LogHistory(TimerAction action, string trigger, string details, bool wasCancelled)
    {
        try
        {
            var settings = _settingsService.Settings;
            settings.History.Insert(0, new HistoryEntry
            {
                Timestamp = DateTime.Now,
                Action = action,
                Trigger = trigger,
                Details = details,
                WasCancelled = wasCancelled
            });

            // Keep last 100 entries
            if (settings.History.Count > 100)
                settings.History.RemoveRange(100, settings.History.Count - 100);

            _ = _settingsService.SaveAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine($"[SystemActionService] History save failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemActionService] Failed to log history: {ex.Message}");
        }
    }

    private void ExecuteImmediate(TimerAction action)
    {
        try
        {
            switch (action)
            {
                case TimerAction.Shutdown:
                    using (Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })) { }
                    break;

                case TimerAction.Restart:
                    using (Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })) { }
                    break;

                case TimerAction.Sleep:
                    Win32.NativeMethods.SetSuspendState(hibernate: false, forceCritical: true, disableWakeEvent: false);
                    break;

                case TimerAction.Hibernate:
                    Win32.NativeMethods.SetSuspendState(hibernate: true, forceCritical: true, disableWakeEvent: false);
                    break;

                case TimerAction.LogOff:
                    Win32.NativeMethods.ExitWindowsEx(Win32.NativeMethods.EWX_LOGOFF, 0);
                    break;

                case TimerAction.Lock:
                    Win32.NativeMethods.LockWorkStation();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemActionService] Failed to execute {action}: {ex.Message}");
            throw;
        }
    }

    public void CancelPendingShutdown()
    {
        CancelWarning();
        try
        {
            using (Process.Start(new ProcessStartInfo("shutdown", "/a")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            })) { }
        }
        catch (Exception ex)
        {
            // No pending shutdown to cancel — this is expected if none was issued
            Debug.WriteLine($"[SystemActionService] CancelPendingShutdown: {ex.Message}");
        }
    }

    private void SendToastNotification(TimerAction action, int seconds)
    {
        try
        {
            // Use the WinRT ToastNotification API directly — no PowerShell subprocess needed.
            // ToastNotificationManager is safe to use from a WinUI 3 unpackaged app.
            var title = "Shutdown Timer Advanced";
            var message = $"{action} will execute in {seconds} seconds. Save your work!";

            // Build XML with actions for Windows 11 interactive experience
            var xml = new XmlDocument();
            xml.LoadXml(
                "<toast launch='action=view'>" +
                "<visual><binding template='ToastText02'>" +
                "<text id='1'/><text id='2'/>" +
                "</binding></visual>" +
                "<actions>" +
                "<action content='Postpone (5m)' arguments='action=postpone&amp;minutes=5' activationType='foreground'/>" +
                "<action content='Cancel' arguments='action=cancel' activationType='foreground'/>" +
                "</actions>" +
                "</toast>");

            xml.SelectSingleNode("//text[@id='1']")!.InnerText = title;
            xml.SelectSingleNode("//text[@id='2']")!.InnerText = message;

            var toast = new ToastNotification(xml);
            ToastNotificationManager.GetDefault().CreateToastNotifier().Show(toast);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SystemActionService] Toast notification failed: {ex.Message}");
        }
    }
}
