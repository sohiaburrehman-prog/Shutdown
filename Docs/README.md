# Shutdown Timer Advanced

A modern Windows shutdown/restart/sleep utility built with WinUI 3 and .NET 9, featuring a dark glassmorphism UI.

## Features

- **Countdown Timer** — Set hours/minutes/seconds, choose an action (shutdown, restart, sleep, hibernate, log off), and let it count down.
- **Process Monitor** — Select any running application and automatically trigger an action when it closes. Perfect for shutting down after a game, render, or long task.
- **Idle Detection** — Monitors mouse and keyboard inactivity via Win32 `GetLastInputInfo`. Triggers your chosen action after a configurable idle threshold.
- **Recurring Schedules** — Set up cron-based schedules with a friendly day-of-week + time picker. Persisted to JSON in AppData.
- **System Tray** — Minimizes to tray with right-click context menu for quick actions. Tooltip shows remaining countdown time.
- **Dashboard** — At-a-glance overview of all active timers, monitors, and schedules.
- **Theme Support** — System (follows Windows), Light, or Dark theme with live switching.

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

## License

See [EULA.md](EULA.md) for the full license agreement.
