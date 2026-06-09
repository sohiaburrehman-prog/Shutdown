# Shutdown Timer Advanced Changelog

## Version 1.4.2

- Fixed Windows 11 tray icons staying hidden after the startup notification (auto-promotes `NotifyIconSettings`).
- Added a high-contrast native tray icon and a setup dialog with a link to Taskbar settings when Windows blocks the icon.
- Updated public contact email to sohiab.rehman@pm.me.
- Published source repository publicly on GitHub.

## Version 1.4.1

- Fixed **Start minimized to tray** so it works on every launch, not only when started with `--minimized`.
- Improved startup reliability: window placement clamping, single-instance activation, and deferred tray hide.
- Regenerated app and tray icons programmatically to remove white-border artifacts.
- Added `scripts/generate-icons.py` for reproducible icon builds.

## Version 1.4

- Redesigned the app around a compact dashboard, clearer navigation, and stronger Shutdown Timer Advanced branding.
- Added practical templates for countdown, idle, and schedule workflows so common use cases are one click away.
- Added battery automation for low and critical thresholds, with configurable actions and an unplugged-only safety option.
- Improved the dashboard command center with trigger context, next-action details, active automation summaries, and a cancel shortcut.
- Improved schedules with clearer next-run and last-run text, health status, and duplicate controls.
- Improved activity history with filters, friendlier timestamps, and clearer completed/cancelled status.
- Added safer warning execution paths for quick actions and battery-triggered automations.
