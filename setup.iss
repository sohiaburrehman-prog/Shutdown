; ============================================================
;  Shutdown Timer - Inno Setup Script
;  Creates a standard Windows installer (.exe)
; ============================================================
;
;  Prerequisites:
;    1. Build the Release first (build-installer.bat does this)
;    2. Install Inno Setup from https://jrsoftware.org/isinfo.php
;    3. Run this script from Inno Setup Compiler
;       or use: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
;
; ============================================================

#define MyAppName "Shutdown Timer"
#define MyAppVersion "1.4.3"
#define MyAppPublisher "Sohiab"
#define MyAppExeName "ShutdownTimer.exe"
#define MyAppURL "https://apps.microsoft.com/detail/9NW80PKZNS4Z"

; Path to the Release build output
#define BuildOutput "bin\x64\Release\net9.0-windows10.0.22621.0"

[Setup]
AppId={{B7E3F2A1-9C4D-4E8F-A5B6-7D2E1F3C8A90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=Docs\EULA.md
PrivilegesRequired=lowest
OutputDir=Installer
OutputBaseFilename=ShutdownTimer-Setup-v{#MyAppVersion}
SetupIconFile=Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry"; Description: "Start with Windows"; GroupDescription: "Other:"; Flags: unchecked

[Files]
; Main application files from Release build
Source: "{#BuildOutput}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Legal documents
Source: "Docs\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Docs\EULA.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Docs\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Optional startup entry (only if user selects the task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ShutdownTimerAdvanced"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startupentry

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{cmd}'), '/C taskkill /IM "{#MyAppExeName}" /T /F >nul 2>&1', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
