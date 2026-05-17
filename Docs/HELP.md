# Shutdown Timer Advanced - User Guide

**Version 1.0**

---

## Overview

Shutdown Timer Advanced is a Windows desktop application that gives you precise control over when your computer shuts down, restarts, sleeps, hibernates, or logs off. Whether you want a simple countdown, automatic shutdown after an app closes, idle-based triggers, or recurring schedules, Shutdown Timer Advanced handles it all from a modern, dark-themed interface.

---

## Getting Started

When you first launch Shutdown Timer Advanced, you'll see the **Dashboard** — a central overview showing the status of all active timers, monitors, and schedules. Use the sidebar on the left to navigate between features.

### System Tray

Shutdown Timer Advanced runs in the system tray (notification area) for easy access. When you close the window, the app minimizes to the tray instead of exiting. You can:

- **Left-click** the tray icon to restore the window.
- **Right-click** the tray icon for quick actions: Show Window, Quick Shutdown/Restart/Sleep/Hibernate/Log Off, or Exit.

To fully exit the application, right-click the tray icon and select **Exit**.

---

## Features

### Countdown Timer

Set a specific duration and choose what happens when time runs out.

1. Navigate to **Countdown** in the sidebar.
2. Enter the desired time using the **Hours**, **Minutes**, and **Seconds** fields.
3. Select an **Action** from the dropdown: Shutdown, Restart, Sleep, Hibernate, or Log Off.
4. Click **Start** to begin the countdown.

**Controls:**

- **Pause** — Temporarily halts the countdown. The remaining time is preserved.
- **Resume** — Continues a paused countdown from where it left off.
- **Cancel** — Stops the countdown and resets the timer.

The large timer display shows your remaining time, and a progress bar tracks completion.

---

### Process Monitor

Automatically trigger an action when a specific application closes. This is useful for shutting down after a game finishes, a render completes, or a long-running task ends.

1. Navigate to **Process Monitor** in the sidebar.
2. The process list loads automatically. Use the **Search** box to filter by name.
3. Click **Refresh** if you launched an app after opening this page.
4. Select the process you want to monitor from the list.
5. Choose the **Action on exit** from the dropdown.
6. Click **Start Monitoring**.

The app will poll the selected process every 500 milliseconds. When the process exits (i.e., the application closes), the selected action triggers automatically.

**Tips:**

- Only processes with a visible window are shown, which filters out background services.
- The PID (Process ID) is shown alongside each entry for disambiguation.
- If a process has multiple windows, it appears once with its primary window title.

---

### Idle Detection

Trigger an action after your computer has been idle (no mouse or keyboard input) for a specified duration.

1. Navigate to **Idle Detection** in the sidebar.
2. Set the **Idle threshold** in minutes (e.g., 30 minutes).
3. Select the desired **Action**.
4. Click **Start Monitoring**.

The app uses the Windows `GetLastInputInfo` API to detect system-wide keyboard and mouse inactivity. The current idle time is displayed in real time, along with a progress bar showing how close you are to the threshold.

**How it works:**

- Idle time resets to zero the moment you move the mouse or press any key.
- The check runs every 3 seconds for efficiency.
- This monitors global input, not just input to the Shutdown Timer Advanced window.

---

### Schedule

Set up recurring schedules to automatically perform actions at specific times on specific days.

1. Navigate to **Schedule** in the sidebar.
2. In the **Add Schedule** section, set the **Hour** (0-23) and **Minute** (0-59).
3. Select which **days of the week** the schedule should run.
4. Choose the **Action** to perform.
5. Click **Add Schedule**.

**Managing schedules:**

- Use the **toggle switch** next to each schedule to enable or disable it without deleting it.
- Click **Remove** to permanently delete a schedule.
- Schedules are saved to disk and persist between app restarts.

**Technical details:**

- Schedules are stored as cron expressions internally and checked every 30 seconds.
- The schedule service runs in the background even when the window is minimized to the tray.
- Duplicate executions are prevented by tracking the last run time.

---

### Dashboard

The Dashboard provides a quick overview of everything that's active:

- **Active Timer** — Shows the current countdown with remaining time.
- **Process Monitor** — Displays which process is being watched and for how long.
- **Idle Detection** — Shows current idle time and monitoring status.
- **Schedule** — Displays how many schedules are active and when the next one will trigger.
- **Quick Actions** — Instant buttons for Sleep, Restart, and Shutdown.

---

## Settings

Access settings via the **Settings** option in the sidebar (gear icon at the bottom).

### Appearance

- **Theme** — Choose between System (follows Windows setting), Light, or Dark. Changes apply immediately across all pages.

### Tray Behavior

- **Minimize to tray on close** — When enabled (default), closing the window hides the app to the system tray. When disabled, closing the window exits the app entirely.
- **Start minimized to tray** — When enabled, the app starts hidden in the tray without showing the window.

### Notifications

- **Show warning before action** — When enabled, a warning countdown appears before the scheduled action executes, giving you a chance to cancel.
- **Warning time (seconds)** — How many seconds before the action to show the warning (default: 30 seconds).

### Defaults

- **Default action** — The pre-selected action when opening timer/monitor features.

---

## Actions Reference

| Action | What It Does |
|--------|-------------|
| **Shutdown** | Powers off the computer completely. Uses `shutdown.exe /s /t 0`. |
| **Restart** | Restarts the computer. Uses `shutdown.exe /r /t 0`. |
| **Sleep** | Puts the computer into a low-power sleep state. RAM stays powered. |
| **Hibernate** | Saves the current state to disk and powers off completely. Resumes where you left off. |
| **Log Off** | Signs out the current user without shutting down the computer. |

**Important:** All actions except Sleep may result in loss of unsaved work. Always save your files before setting up automated actions.

---

## Keyboard Shortcuts

The application follows standard Windows keyboard conventions. All navigation and controls are accessible via Tab and Enter keys.

---

## Troubleshooting

**The app won't shut down my computer:**
Some organizations restrict shutdown commands via Group Policy. Try running the app as Administrator if shutdown fails.

**Process Monitor doesn't show my app:**
Only processes with a visible main window are listed. Background processes, services, and console applications without a GUI window won't appear. Click Refresh after launching the target app.

**Idle detection triggers too early:**
Ensure no other software is simulating mouse or keyboard input (such as presentation tools or screen savers). The idle timer resets on any system-wide input event.

**Schedules don't run:**
Make sure the app is running (check the system tray). Schedules only execute while Shutdown Timer Advanced is active. The schedule must be toggled **on** (enabled).

**The window disappeared:**
The app is likely minimized to the system tray. Look for the Shutdown Timer Advanced icon in the notification area (bottom-right of the taskbar). You may need to click the "Show hidden icons" arrow (^) to find it.

---

## System Requirements

- Windows 10 version 1809 (build 17763) or later
- Windows 11 (recommended for best visual experience with Mica backdrop)
- .NET 9 Runtime
- x64 or ARM64 processor

---

## Uninstalling

If installed from the Microsoft Store, uninstall via Settings > Apps > Shutdown Timer Advanced > Uninstall.

If running as a standalone application, simply delete the application folder. Settings are stored in `%AppData%\ShutdownTimer\` and can be deleted manually to remove all configuration data.

---

## Support

For bug reports, feature requests, or questions, please use the Microsoft Store review/feedback system or contact the developer through the official support channels listed on the Store page.
