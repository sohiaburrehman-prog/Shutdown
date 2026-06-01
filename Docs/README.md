# Shutdown Timer Advanced — Developer Guide

A modern Windows shutdown/restart/sleep utility built with WinUI 3 and .NET 9.

For the public project overview, see the [root README](../README.md).

## Features (v1.4)

- **Countdown Timer** — Set hours/minutes/seconds with guided templates (movie night, update restart, long download).
- **Process Monitor** — Trigger an action when a selected app exits or when a named process starts.
- **Idle Detection** — Monitors input inactivity via Win32 `GetLastInputInfo` with desk-break and bedtime templates.
- **Schedules** — Cron-based recurring or one-time rules with next/last run, health status, and duplicate controls.
- **Battery Automation** — Low and critical battery thresholds with configurable actions (Settings).
- **Dashboard** — Next action, trigger context, active automations, battery insight, and cancel shortcut.
- **Activity Log** — Filterable history of executed and cancelled actions.
- **System Tray** — Minimize to tray, quick actions, optional start with Windows.
- **Safety Warnings** — Shared warning countdown with cancel/postpone before destructive actions.
- **Theme Support** — System, Light, or Dark theme with live switching.

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | WinUI 3 (Windows App SDK 1.6) |
| Runtime | .NET 9 |
| Architecture | MVVM with CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| System Tray | H.NotifyIcon.WinUI |
| Scheduling | NCrontab |
| Persistence | System.Text.Json |
| Win32 Interop | P/Invoke (GetLastInputInfo, SetSuspendState, ExitWindowsEx) |

## Project Structure

```
ShutdownTimer/
├── Models/          # Data models and enums
├── ViewModels/      # MVVM ViewModels (CommunityToolkit.Mvvm source generators)
├── Views/           # XAML pages and MainWindow
├── Services/        # Business logic (Timer, ProcessMonitor, IdleDetection, Schedule, SystemAction, Settings)
├── Win32/           # P/Invoke declarations
├── Helpers/         # Converters and utilities
└── Resources/       # Icons and assets
```

## Building

### Prerequisites

- Visual Studio 2022 (17.8+)
- .NET 9 SDK
- Windows App SDK workload
- Windows 10 SDK (10.0.22621.0)

### Steps

1. Clone the repository
2. Open `ShutdownTimer.sln` in Visual Studio
3. Restore NuGet packages
4. Build for x64 (Debug or Release)
5. Run

### Unpackaged Mode

The app runs in unpackaged mode (`<WindowsPackageType>None</WindowsPackageType>`) for easy development. For Microsoft Store submission, you'll need to switch to MSIX packaging.

## Configuration

Settings are stored in `%AppData%\ShutdownTimer\settings.json`. Schedules are stored alongside in the same directory.

## Legal and security

| Document | Purpose |
|----------|---------|
| [EULA.md](EULA.md) | End-user license for compiled releases |
| [PRIVACY.md](PRIVACY.md) | Privacy policy (local-only data) |
| [CHANGELOG.md](CHANGELOG.md) | Release notes |
| [../LICENSE](../LICENSE) | Source code license (proprietary) |
| [../SECURITY.md](../SECURITY.md) | Security policy and vulnerability reporting |
