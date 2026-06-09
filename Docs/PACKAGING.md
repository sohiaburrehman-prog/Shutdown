# Shutdown Timer — MSIX Packaging Guide

## Prerequisites

- **Windows 10/11** (build 17763+)
- **.NET 9 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Windows 10 SDK** — [Download](https://developer.microsoft.com/windows/downloads/windows-sdk/) (provides `makeappx.exe` and `signtool.exe`)
- **PowerShell 5.1+** (included with Windows)

## Quick Start

Open PowerShell in the project root and run:

```powershell
.\build-msix.ps1
```

This will:
1. Generate a self-signed certificate (first run only)
2. Build the app in Release mode
3. Create the MSIX package
4. Sign it with the certificate

The output file will be: `ShutdownTimer_1.4.2.0_x64.msix`

## Installing Locally

Before installing a self-signed MSIX, you need to trust the certificate once:

```powershell
# Run as Administrator
Import-Certificate -FilePath ".\certs\ShutdownTimer.cer" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
```

Then double-click the `.msix` file to install.

## Build Options

```powershell
# Build for ARM64
.\build-msix.ps1 -Platform ARM64

# Skip build, repackage existing output
.\build-msix.ps1 -SkipBuild

# Custom certificate password
.\build-msix.ps1 -CertPassword "YourPassword123"
```

## Microsoft Store Submission

1. Register at [Microsoft Partner Center](https://partner.microsoft.com)
2. Reserve the app name "Shutdown Timer"
3. Create a new submission
4. Upload the `.msix` package
5. The Store will re-sign it with Microsoft's certificate
6. Set pricing ($2.99–$4.99 suggested)
7. Fill in the Store listing using content from `README.md`
8. Use the assets in `Assets/` for Store screenshots

**Note:** For Store submission, the `Publisher` in `Package.appxmanifest` must match your Partner Center publisher identity. Update the `CN=ShutdownTimer` value to match what Microsoft assigns you.

## Project Structure

```
ShutdownTimer/
├── Assets/                    ← MSIX visual assets (29 PNGs)
│   ├── Square44x44Logo.*      ← App list icons
│   ├── Square150x150Logo.*    ← Start menu tile
│   ├── Wide310x150Logo.*      ← Wide tile
│   ├── StoreLogo.*            ← Store listing icon
│   ├── SplashScreen.*         ← App splash screen
│   └── LockScreenLogo.*       ← Lock screen badge
├── Resources/                 ← In-app resources
│   ├── app.ico                ← Executable icon
│   ├── logo48.png             ← Sidebar logo
│   └── TrayIcons/             ← System tray icons
├── Package.appxmanifest       ← MSIX identity & capabilities
├── build-msix.ps1             ← Automated build script
├── certs/                     ← Generated certificates (gitignored)
├── publish/                   ← Build output (gitignored)
└── msix-output/               ← MSIX layout (gitignored)
```

## Updating the Version

1. Update `Version` in `ShutdownTimer.csproj`
2. Update `Version` in `Package.appxmanifest` (the `Identity` element)
3. Run `.\build-msix.ps1`

## Troubleshooting

**"makeappx.exe not found"** — Install the Windows 10 SDK from the link above. The script searches common install locations automatically.

**"The package could not be installed"** — Make sure you've trusted the signing certificate (see Installing Locally section above).

**"Publisher does not match"** — The `Publisher` in `Package.appxmanifest` must exactly match the certificate subject. For self-signed certs, both use `CN=ShutdownTimer` by default.

**Store submission rejected** — Update the `Publisher` in the manifest to match your Partner Center identity (format: `CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`).
