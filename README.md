# Shutdown Timer Advanced

A modern Windows power-management utility built with **WinUI 3** and **.NET 9**. Schedule shutdowns, monitor processes, detect idle time, automate battery thresholds, and manage everything from a compact dashboard with system-tray support.

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Get%20app-0078D4?logo=microsoft)](https://apps.microsoft.com/detail/9NW80PKZNS4Z)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/sohiaburrehman-prog/Shutdown)

## Features

- **Countdown timer** — Hours, minutes, and seconds with shutdown, restart, sleep, hibernate, log off, or lock.
- **Process monitor** — Trigger an action when a selected app exits or when a named process starts.
- **Idle detection** — Act after configurable keyboard/mouse inactivity.
- **Schedules** — Recurring or one-time cron-based rules with next-run visibility and duplicate controls.
- **Battery automation** — Low and critical battery thresholds with configurable actions (Advanced).
- **Dashboard** — Next action, active automations, battery insight, and quick cancel.
- **Activity log** — Filterable history of executed and cancelled actions.
- **System tray** — Minimize to tray, quick actions, and optional start with Windows.
- **Safety warnings** — Shared warning countdown with cancel/postpone before destructive actions.

## Screenshots

Store and marketing assets live in [`Assets/`](Assets/) and the repository root (`StoreLogo_*.png`, `StorePoster_*.png`).

## Requirements

- Windows 10 version 1809 (build 17763) or later / Windows 11
- x64 or ARM64

## Getting the app

| Channel | Notes |
|---------|--------|
| [Microsoft Store](https://apps.microsoft.com/detail/9NW80PKZNS4Z) | Recommended for most users |
| Build from source | See [Building](#building) below |
| Installer (maintainers) | Run `build-installer.bat` after a Release build |

## Building

### Prerequisites

- Visual Studio 2022 (17.8+) with the **Windows App SDK** workload
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10 SDK (10.0.22621.0)

### Quick build

```powershell
git clone https://github.com/sohiaburrehman-prog/Shutdown.git
cd Shutdown
dotnet build ShutdownTimer.csproj -c Release -r win-x64
dotnet test ShutdownTimer.Tests\ShutdownTimer.Tests.csproj
```

For MSIX packaging and Store submission, see [`Docs/PACKAGING.md`](Docs/PACKAGING.md) and [`Docs/STORE_SUBMISSION.md`](Docs/STORE_SUBMISSION.md).

The app runs unpackaged during development (`WindowsPackageType=None` in the project file).

## Configuration

Settings and schedules are stored locally at:

```text
%AppData%\ShutdownTimer\settings.json
```

An HMAC integrity check detects tampering with `settings.json` and resets to safe defaults if verification fails.

No cloud sync, telemetry, or external data transmission is performed. See [`Docs/PRIVACY.md`](Docs/PRIVACY.md).

## Documentation

| Document | Purpose |
|----------|---------|
| [`Docs/README.md`](Docs/README.md) | Developer-oriented project overview |
| [`Docs/HELP.md`](Docs/HELP.md) | In-app help content (user guide) |
| [`Docs/CHANGELOG.md`](Docs/CHANGELOG.md) | Release notes |
| [`Docs/PRIVACY.md`](Docs/PRIVACY.md) | Privacy policy |
| [`Docs/EULA.md`](Docs/EULA.md) | End-user license agreement |
| [`Docs/PACKAGING.md`](Docs/PACKAGING.md) | MSIX build guide |
| [`Docs/THIRD_PARTY_NOTICES.md`](Docs/THIRD_PARTY_NOTICES.md) | Open-source dependency licenses |
| [`SECURITY.md`](SECURITY.md) | Security policy and vulnerability reporting |

## Security

- Runs at **standard user** level (`asInvoker`); no admin required for normal use.
- Settings and history remain **on device only**; `settings.json` is protected by a per-user integrity check.
- Power actions use documented Windows APIs with user-configurable warnings.

Report security issues privately — do **not** open public GitHub issues for vulnerabilities. See [`SECURITY.md`](SECURITY.md).

## License

**Copyright © 2026 Sohiab. All rights reserved.**

- **Application use** is governed by the [End-User License Agreement](Docs/EULA.md).
- **Source code** in this repository is published for transparency and review only. Viewing or cloning the repository does **not** grant rights to copy, modify, distribute, or sublicense the software. See [LICENSE](LICENSE).

## Store vs source publisher names

The Microsoft Store package identity may display **Providence** as the publisher (`Package.appxmanifest`). Installer and source releases use **Sohiab** as the developer/publisher name. Both refer to the same product.

## Contact

- **Developer:** Sohiab
- **Email:** sohiab@outlook.com
- **Store:** [Shutdown Timer Advanced on Microsoft Store](https://apps.microsoft.com/detail/9NW80PKZNS4Z)

## Contributing

This is proprietary software. Public issue reports for bugs and feature requests are welcome; pull requests may not be accepted unless agreed in advance. See [`CONTRIBUTING.md`](CONTRIBUTING.md).
