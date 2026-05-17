# Shutdown Timer Advanced — Audit Report

**Date:** March 21, 2026
**Scope:** Error review, GDPR compliance, Security requirements
**Version audited:** 1.1.0.0

---

## 1. Code Errors & Runtime Issues

### Critical

**1.1 — `async void` in SystemActionService.RunPreActionAndExecute()**
The pre-action program runner uses `async void`, which is a dangerous pattern in C#. If an unhandled exception occurs inside this method, it will crash the entire application with no way to catch it. This should be `async Task` with a top-level try/catch, or the caller should use `Task.Run` with proper error handling.

**1.2 — Idle time calculation overflow in NativeMethods.cs**
`Environment.TickCount64` returns a `long`, but the idle time calculation casts it to `uint` via `unchecked`. After ~49 days of uptime, the `dwTime` field (which is `uint`) wraps around, and the subtraction produces incorrect results. The fix is to use `(uint)(Environment.TickCount64 - info.dwTime)` or, better, compute everything in `long` and clamp.

**1.3 — Missing null guard on `_actionService` in MainWindow constructor**
`App.GetService<ISystemActionService>()` is called and immediately used to subscribe to events without a null check. If DI resolution fails, the app crashes on startup.

### High

**1.4 — MiniWindow event subscription leak**
`MiniWindow` subscribes to `TimerService.Tick` and `Completed` in the constructor but only unsubscribes in `CloseBtn_Click`. If the window is closed by any other means (e.g., `App.Exit`, OS shutdown), the subscriptions are never removed, causing a memory leak and potential crashes from callbacks on a disposed window. Fix: unsubscribe in the `Closed` event handler instead.

**1.5 — Fire-and-forget task in ProcessMonitorService**
`_ = MonitorExitAsync(...)` discards the task. If an unhandled exception occurs inside, it becomes an unobserved task exception which can crash the app in certain .NET configurations. Wrap in try/catch or use `ContinueWith` to log errors.

**1.6 — Tray menu assumes `_mainWindow` is non-null**
In `App.xaml.cs`, the tray menu click handlers reference `_mainWindow` directly. If the tray icon initialises before the main window (race condition on slow startup), this throws a `NullReferenceException`.

**1.7 — WindowInterop.SetWindowLongPtr result not validated**
If the WndProc subclassing call fails, `_origWndProc` remains `IntPtr.Zero`, and every subsequent `CallWindowProc` call will crash or behave unpredictably.

### Medium

**1.8 — DateTime.Now vs DateTime.UtcNow inconsistency**
`ScheduleService` uses `DateTime.Now` (local time) while `TimerService` uses `DateTime.UtcNow`. During DST transitions, schedules could fire an hour early or late, or skip entirely. Pick one and be consistent — for a desktop timer app, `DateTime.Now` everywhere is the simpler choice since users think in local time.

**1.9 — Silent catch-all in ScheduleService.CheckSchedulesAsync**
Invalid cron expressions are caught with a bare `catch { }` and silently skipped. Users have no way to know their schedule has a bad expression. At minimum, log the error and surface it in the UI.

**1.10 — CountdownTimerPage animation timer runs when not needed**
The `DispatcherTimer` for the pulsing animation runs continuously at 1500ms intervals even when `IsRunning` is false. The tick handler checks `IsRunning` and returns early, but the timer itself should be started/stopped based on the running state to avoid unnecessary CPU wake-ups (matters for battery life on laptops).

**1.11 — No user feedback on zero-duration timer start**
`CountdownTimerViewModel.Start()` silently returns if duration is zero. The user clicks Start and nothing happens with no explanation.

**1.12 — History not paginated**
All 100 history entries are loaded into an `ObservableCollection` at once. This is fine for 100 items, but the cap should be enforced on read as well as write (currently only capped on write in `LogHistory`).

---

## 2. GDPR Compliance

### Status: Largely Compliant — Minor Updates Needed

The app stores all data locally in `%APPDATA%/ShutdownTimer/settings.json` with no network communication of any kind. This is inherently privacy-friendly. However, the new features introduced in v1.1 create a few gaps in the existing privacy policy.

**2.1 — Privacy policy doesn't mention Activity History (HIGH)**
The new History feature logs action timestamps, trigger types, action names, and success/failure status to the local settings file. While this isn't PII, the privacy policy's "Local Data Storage" section only mentions "user preferences and settings." Activity history is behavioural data (when the user's computer was shut down, when they were idle, etc.) and should be explicitly disclosed.

**Recommended update** to the privacy policy's Local Data Storage section:
> Shutdown Timer Advanced stores user preferences, settings, and an optional activity log (recording action type, trigger, timestamp, and success status) locally in the application data directory. No data leaves your device. The activity log can be cleared at any time from the Activity Log page.

**2.2 — Privacy policy doesn't mention Lock action**
The "System Actions and Safety" section lists Shutdown, Restart, Sleep, Hibernate, and Log Off but not the new Lock action. This should be added.

**2.3 — Privacy policy doesn't mention Pre-Action Program execution**
The app can now launch arbitrary external programs before performing a system action. This is a significant capability that should be disclosed under "System Actions and Safety."

**2.4 — Privacy policy doesn't mention Process Monitoring details**
The new "Wait for Start" mode and CPU/memory data collection (even though transient and not persisted) should be mentioned since the app is observing running processes on the system.

**2.5 — No data export or deletion mechanism beyond uninstall**
GDPR Article 17 (Right to Erasure) is satisfied by the ability to clear history and uninstall. However, it would be good practice to add a "Clear All Data" button in Settings that resets settings.json to defaults, providing an easy in-app path to full data erasure.

**2.6 — History entries contain timestamps that could be considered behavioural data**
While timestamps alone aren't PII, a log of "computer shutdown at 2:00 AM every night" constitutes a usage pattern. Under strict GDPR interpretation, this is personal data if it can be linked to a natural person (which it can, since it's on their personal computer). The mitigation is that it never leaves the device, but the disclosure in the privacy policy should be explicit.

---

## 3. Security Review

### Critical

**3.1 — PowerShell injection in toast notifications**
`SendToastNotification()` builds a PowerShell command string using string interpolation with the `title` and `message` parameters. While single quotes are escaped (`'` → `''`), this is insufficient. A crafted string containing `$(...)` or backticks could execute arbitrary PowerShell commands.

**Example attack vector:** If any future code path allows user-controlled text to reach the title/message (e.g., a process name containing `$(Remove-Item -Recurse C:\)`), it would execute.

**Fix:** Use `ProcessStartInfo` with arguments passed via stdin or a temp file, not string interpolation. Alternatively, use the Windows App SDK notification APIs directly instead of shelling out to PowerShell.

**3.2 — Pre-Action Program path has no validation**
`PreActionProgramPath` accepts any file path and launches it via `Process.Start()`. There is no validation that:
- The path points to an executable (not a script or batch file that could be hijacked)
- The path doesn't contain path traversal characters
- The file actually exists before attempting execution
- The file is in a trusted location

While the user explicitly configures this path, a corrupted or tampered `settings.json` could cause arbitrary program execution on next app launch. Consider validating the path on load and warning if it points to a script or unknown location.

### High

**3.3 — Settings file has no integrity protection**
`settings.json` in `%APPDATA%` is readable and writable by any process running as the user. A malicious program could modify `PreActionProgramPath` to point to malware, and the next time the timer fires, the app would dutifully launch it. Consider adding a checksum or HMAC to detect tampering (though this is only defence-in-depth since the key would also be local).

**3.4 — P/Invoke declarations should use `[DefaultDllImportSearchPaths]`**
The `LockWorkStation`, `SetSuspendState`, `ExitWindowsEx`, and `GetLastInputInfo` P/Invoke declarations don't specify DLL search paths. While these are well-known system DLLs, adding `[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]` prevents DLL planting attacks where a malicious DLL with the same name is placed in the application directory.

### Medium

**3.5 — Process list exposes system-wide information**
`GetRunningProcesses()` calls `Process.GetProcesses()` which returns all processes visible to the user, including those from other applications. The memory and CPU data collected could be considered sensitive. This data is transient (not persisted), but it's displayed in the UI where screen-sharing or screenshots could expose it.

**3.6 — No rate limiting on system actions**
There's no guard against rapid repeated execution of system actions. If a bug in the schedule service or process monitor causes rapid-fire triggers, the app could repeatedly attempt shutdown/restart/lock. Consider adding a cooldown period (e.g., 60 seconds) between consecutive action executions.

**3.7 — Toast notification uses PowerShell subprocess**
Spawning PowerShell for notifications is a security concern because:
- It may trigger antivirus/EDR alerts
- It creates a visible (if brief) PowerShell process
- Corporate environments often restrict PowerShell execution

Consider using `Microsoft.Windows.AppNotifications` from the Windows App SDK instead, which is the supported way to show toasts from WinUI 3 apps.

---

## 4. Recommendations Summary

| # | Issue | Severity | Effort |
|---|-------|----------|--------|
| 3.1 | PowerShell injection in toasts | Critical | Medium — switch to AppNotifications API |
| 1.1 | async void crash risk | Critical | Low — change to async Task + try/catch |
| 1.2 | Idle time overflow | Critical | Low — fix arithmetic |
| 2.1 | Privacy policy update for History | High | Low — text update |
| 3.2 | Pre-action path validation | High | Low — add file existence + extension check |
| 3.4 | P/Invoke DLL search paths | High | Low — add attribute |
| 1.4 | MiniWindow event leak | High | Low — move to Closed handler |
| 1.5 | Unobserved task exception | High | Low — add try/catch |
| 2.2–2.4 | Privacy policy gaps | Medium | Low — text updates |
| 1.8 | DateTime inconsistency | Medium | Medium — audit all DateTime usage |
| 3.6 | Action rate limiting | Medium | Low — add cooldown timer |
| 1.9 | Silent cron errors | Medium | Low — add logging + UI feedback |
| 3.7 | Replace PowerShell toasts | Medium | Medium — use AppNotifications |

---

## 5. Privacy Policy — Suggested Updated Sections

### Local Data Storage (replace existing)

> **Shutdown Timer Advanced:** User preferences, settings, and an activity log are stored locally in the application data directory (`%APPDATA%\ShutdownTimer`). The activity log records action type, trigger method, timestamp, and success/failure status for your reference. No data leaves your device. You can clear the activity log at any time from the Activity Log page, or delete all data by uninstalling the application.

### System Actions and Safety (replace existing list)

> - **Shutdown** — Powers off the computer
> - **Restart** — Restarts the computer
> - **Sleep** — Puts the computer into sleep mode
> - **Hibernate** — Puts the computer into hibernation (Advanced only)
> - **Log Off** — Signs out the current user (Advanced only)
> - **Lock** — Locks the workstation (Advanced only)
>
> **Pre-action programs:** The Advanced version can optionally run a user-specified program before executing any action, allowing you to save work or run cleanup scripts. This program path is configured by you and stored locally.

### Process Monitoring (new subsection under Background behavior)

> The Advanced version's process monitoring feature can observe running processes to detect when a specific program exits or starts. It also displays memory usage of running processes while the monitoring page is open. This information is transient and is never stored or transmitted.
