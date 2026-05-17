# Microsoft Store Submission Guide

## Shutdown Timer (Pro) and Shutdown Timer Lite

---

## Pre-Submission Checklist

### Privacy and Legal (Required)

- [x] Privacy Policy document (PRIVACY.md) included in build output
- [x] EULA document (EULA.md) included in build output
- [x] GDPR compliance section in EULA (Section 8)
- [x] UK Data Protection Act compliance in privacy policy
- [x] In-app access to Privacy Policy (Settings > Legal & Privacy)
- [x] In-app access to EULA (Settings > Legal & Privacy)
- [x] No personal data collected (declare "Data Not Collected" in Store listing)
- [x] No admin privileges required (asInvoker in app.manifest)

### Privacy Policy URL

The Microsoft Store requires a Privacy Policy URL when submitting. Options:

1. **GitHub Pages (free, recommended):**
   - Create a GitHub repo (e.g., `sohiab/shutdown-timer-privacy`)
   - Add PRIVACY.md as index.md
   - Enable GitHub Pages in repo Settings
   - URL will be: `https://sohiab.github.io/shutdown-timer-privacy/`

2. **Simple HTML page on any host:**
   - Convert PRIVACY.md to HTML
   - Host on any web server or free hosting (Netlify, Vercel, etc.)

3. **Direct link to a GitHub file:**
   - Less professional but acceptable
   - e.g., `https://github.com/sohiab/shutdown-timer/blob/main/PRIVACY.md`

Use the SAME URL for both the Pro and Lite store listings.

### Store Data Disclosure

When the Store submission form asks about data collection, select:

- **Data collected:** None / Data Not Collected
- **Data shared with third parties:** No
- **Data used for tracking:** No
- **Data used for advertising:** No

This is the simplest category and requires no further detail.

---

## Store Listing Content

### Shutdown Timer (Pro) - Paid

**App name:** Shutdown Timer
**Category:** Utilities & tools
**Price:** [Set your price]

**Short description (max 100 chars):**
Schedule shutdowns, restarts, sleep, and more with a modern Windows timer app.

**Description:**
Shutdown Timer is a modern Windows power management tool that lets you schedule
system actions with precision and flexibility.

Features:
- Countdown timer with hours, minutes, and seconds
- Multiple actions: Shutdown, Restart, Sleep, Hibernate, Log Off
- Process monitoring: trigger actions when a specific process ends
- Idle detection: act when your PC has been idle for a set duration
- Cron-based scheduling for recurring actions
- Configurable warning notifications before execution
- System tray integration for background operation
- Modern WinUI 3 interface with glassmorphism design
- Dark theme throughout

Privacy: This app collects no personal data whatsoever. All settings are
stored locally on your device. No analytics, no telemetry, no ads.

**Privacy policy URL:** [Your hosted privacy policy URL]
**Website:** [Optional]
**Support contact:** sohiab@outlook.com

---

### Shutdown Timer Lite - Free

**App name:** Shutdown Timer Lite
**Category:** Utilities & tools
**Price:** Free

**Short description (max 100 chars):**
Simple shutdown timer for Windows. Set a countdown to shut down, restart, or sleep your PC.

**Description:**
Shutdown Timer Lite is a simple, free timer that lets you schedule your PC to
shut down, restart, or sleep after a countdown.

Features:
- Set countdown in hours, minutes, and seconds
- Three actions: Shutdown, Restart, Sleep
- Large countdown display
- Cancel any time before execution
- Clean, minimal WinUI 3 interface
- Dark theme

No bloat, no ads, no data collection. Just a timer that does what it says.

Want more features? Upgrade to Shutdown Timer Pro for process monitoring,
idle detection, scheduling, system tray integration, and more.

**Privacy policy URL:** [Same URL as Pro]
**Website:** [Optional]
**Support contact:** sohiab@outlook.com

---

## Age Rating

When asked about age rating content:

- **Violence:** None
- **Fear:** None
- **Mature content:** None
- **Gambling:** None
- **User interaction:** None
- **Data sharing:** None

This should qualify for a **3+** or **Everyone** rating.

---

## Certification Notes

If Microsoft requests additional info during review, key points:

1. **Why does the app call shutdown.exe?**
   This is a power management timer. The app uses the Windows `shutdown.exe`
   command to perform scheduled shutdowns and restarts. Sleep/hibernate use
   the `SetSuspendState` Win32 API via powrprof.dll. These are standard
   Windows power management interfaces.

2. **Does the app require admin privileges?**
   No. The app runs as a standard user (asInvoker). The Windows `shutdown.exe`
   command does not require elevation for the current user's session.

3. **Does the app run in the background?**
   Pro only: Optionally minimizes to system tray (user must enable this in
   Settings). Lite: No background operation.

4. **Does the app auto-start with Windows?**
   No auto-start functionality is included.

---

## Visual Assets Required

The Store requires specific image sizes. Both projects already have MSIX assets
in the Assets/ folder. For Store listing you'll also need:

- **Store logo:** 300x300 PNG
- **Screenshots:** 1366x768 or 1920x1080 (at least 1, recommended 3-5)
- **Wide tile (optional):** 1100x600 PNG
- **Hero image (optional):** 1920x1080

Take screenshots of the app running in both Dark mode showing key features.

---

*Last updated: March 8, 2026*
