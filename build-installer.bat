@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  Shutdown Timer - Installer Builder
::  Builds Release then creates setup.exe via Inno Setup
:: ============================================================

set APP_NAME=ShutdownTimer
set PROJECT=ShutdownTimer.csproj
set CONFIG=Release
set PLATFORM=x64
set FRAMEWORK=net9.0-windows10.0.22621.0

echo.
echo ========================================
echo  %APP_NAME% - Build Installer
echo ========================================
echo.

cd /d "%~dp0"

:: -------------------------------------------
:: Find Visual Studio MSBuild
:: -------------------------------------------
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist %VSWHERE% (
    echo ERROR: vswhere.exe not found. Is Visual Studio installed?
    pause
    exit /b 1
)

for /f "usebackq tokens=*" %%i in (`%VSWHERE% -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    set "MSBUILD=%%i"
)

if not defined MSBUILD (
    echo ERROR: MSBuild not found.
    pause
    exit /b 1
)

echo Found MSBuild: %MSBUILD%

:: -------------------------------------------
:: Find Inno Setup Compiler
:: -------------------------------------------
set "ISCC="
set "ISCC_X86=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set "ISCC_X64=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%ISCC_X86%" set "ISCC=%ISCC_X86%"
if exist "%ISCC_X64%" set "ISCC=%ISCC_X64%"

if not defined ISCC (
    echo.
    echo WARNING: Inno Setup 6 not found.
    echo Download from: https://jrsoftware.org/isdl.php
    echo.
    echo Will build Release only - no installer.
    echo You can run setup.iss manually from Inno Setup later.
    echo.
)

:: -------------------------------------------
:: Restore NuGet packages
:: -------------------------------------------
echo.
echo Restoring packages...
"%MSBUILD%" %PROJECT% /t:Restore /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Package restore failed.
    pause
    exit /b 1
)

:: -------------------------------------------
:: Build Release (unpackaged)
:: -------------------------------------------
echo.
echo Building %CONFIG% (%PLATFORM%)...
"%MSBUILD%" %PROJECT% /t:Build /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /p:WindowsPackageType=None /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)

echo.
echo Release build successful.

:: -------------------------------------------
:: Create zip (portable version)
:: -------------------------------------------
set BUILD_OUT=bin\%PLATFORM%\%CONFIG%\%FRAMEWORK%
set ZIP_NAME=%APP_NAME%-v1.4.2-Portable-%PLATFORM%.zip

if exist "%ZIP_NAME%" del "%ZIP_NAME%"
echo Creating portable zip: %ZIP_NAME%...
powershell -NoProfile -Command "Compress-Archive -Path '%BUILD_OUT%\*' -DestinationPath '%ZIP_NAME%' -Force"

:: -------------------------------------------
:: Run Inno Setup to create installer
:: -------------------------------------------
if defined ISCC (
    echo.
    echo Creating installer with Inno Setup...
    "%ISCC%" setup.iss
    if errorlevel 1 (
        echo ERROR: Inno Setup compilation failed.
        pause
        exit /b 1
    )
)

:: -------------------------------------------
:: Done
:: -------------------------------------------
echo.
echo ========================================
echo  BUILD COMPLETE
echo ========================================
echo.
echo  Portable zip: %ZIP_NAME%
if defined ISCC (
    echo  Installer:    Installer\ShutdownTimer-Setup-v1.4.2.exe
)
echo.
echo  The installer:
echo    - Shows EULA during install
echo    - Installs to Program Files
echo    - Creates Start Menu shortcut
echo    - Optional desktop shortcut
echo    - Optional Start with Windows
echo    - Clean uninstall via Add/Remove Programs
echo.
pause
