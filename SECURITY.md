# Security Policy

## Supported versions

Security fixes are provided for the latest released version available on the [Microsoft Store](https://apps.microsoft.com/detail/9NW80PKZNS4Z) and the latest tagged release in this repository.

| Version | Supported |
|---------|-----------|
| 1.4.x   | Yes       |
| < 1.4   | No        |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, report them privately to:

- **Email:** sohiab.rehman@pm.me
- **Subject:** `[Shutdown Timer Security]`

Include:

1. A clear description of the issue
2. Steps to reproduce
3. Affected version
4. Impact assessment (if known)
5. Proof of concept (if available)

We aim to acknowledge reports within **5 business days** and will work with you on a fix and coordinated disclosure when appropriate.

## Security model

Shutdown Timer Advanced is designed with the following properties:

### Privilege and execution

- The application runs with **standard user privileges** (`asInvoker` in `app.manifest`).
- It does not require administrator elevation for normal operation.
- System power actions are initiated through documented Windows mechanisms (`shutdown.exe`, `SetSuspendState`, `LockWorkStation`, etc.).

### Data handling

- **No telemetry, analytics, or cloud sync.**
- Settings, schedules, and activity history are stored locally under `%AppData%\ShutdownTimer\`.
- `settings.json` is protected by a per-user HMAC integrity check. If tampering is detected, the app resets to safe defaults and shows a warning in Settings.
- Integrity metadata (`.integrity-key`, `settings.integrity`) is stored alongside settings and is removed by **Clear All Data**.
- The app does not open outbound network connections for user data collection.

### User-controlled automation

- Timers, schedules, process monitors, idle rules, and battery automation only run when explicitly configured by the user.
- Warning notifications and cancel/postpone controls are available before destructive actions (when enabled in Settings).
- Pre-action programs are limited to local `.exe`/`.com` files; UNC paths and reparse points are rejected.
- Process monitoring observes running processes only while monitoring is active; transient process metadata is not persisted or transmitted.

### Settings integrity

- `settings.json` is validated on load using an HMAC keyed with a DPAPI-protected secret stored in the same AppData folder.
- If verification fails, settings are reset to defaults and the user is notified. This mitigates casual tampering by other local processes but is not a substitute for full-disk encryption or account isolation.
- **Clear All Data** removes settings, integrity files, schedules, history, and startup registry entries.

### Installer behavior

- The Inno Setup installer closes or terminates a running instance of `ShutdownTimer.exe` before upgrading files to prevent locked-file install failures.

## Out of scope

The following are generally **not** treated as application vulnerabilities:

- Loss of unsaved work caused by user-configured shutdown, restart, sleep, hibernate, or log off actions
- Actions triggered after the user explicitly started a timer or enabled an automation rule
- Elevation prompts or behavior originating from Windows when executing system power commands
- Issues requiring malware, physical access, or compromise of the user account outside this application

## Secure development

- Signing certificates (`.pfx`) and private keys are excluded via `.gitignore`.
- Dependencies are managed through NuGet with version pins in `ShutdownTimer.csproj`.
- Automated tests cover scheduling and core logic in `ShutdownTimer.Tests/`.

## Recommendations for users

- Enable warning notifications before destructive actions.
- Review schedules, idle rules, process monitors, and battery automation before leaving the PC unattended.
- Keep the app updated through the Microsoft Store or official releases.
- Uninstall or clear `%AppData%\ShutdownTimer\` to remove local settings and activity history.
