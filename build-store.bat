@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  Shutdown Timer - Microsoft Store MSIX Package Builder
:: ============================================================
::
::  NOTE: WinUI 3 MSIX builds are most reliable through Visual
::  Studio. If this script fails, use Visual Studio instead:
::    1. Open the solution in Visual Studio
::    2. Right-click project > Publish > Create App Packages
::    3. Select "Microsoft Store" as the target
::    4. Follow the wizard
::
:: ============================================================

set APP_NAME=ShutdownTimer
set PROJECT=ShutdownTimer.csproj
set CONFIG=Release
set PLATFORM=x64

echo.
echo ========================================
echo  %APP_NAME% - Microsoft Store Build
echo ========================================
echo.

cd /d "%~dp0"

:: Find Visual Studio MSBuild
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
echo.

if not exist "Package.appxmanifest" (
    echo ERROR: Package.appxmanifest not found.
    pause
    exit /b 1
)

:: Clean
set MSIX_OUT=%~dp0AppPackages
if exist "%MSIX_OUT%" rmdir /s /q "%MSIX_OUT%"

:: Restore
echo Restoring packages...
"%MSBUILD%" %PROJECT% /t:Restore /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Package restore failed.
    pause
    exit /b 1
)

:: Build MSIX - unset WindowsPackageType and enable MSIX tooling
echo.
echo Building MSIX package for Store submission...
"%MSBUILD%" %PROJECT% /t:Rebuild /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /p:WindowsPackageType=MSIX /p:EnableMsixTooling=true /p:AppxPackage=true /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true /p:AppxPackageDir=%MSIX_OUT%\ /p:AppxPackageSigningEnabled=false /p:UapAppxPackageBuildMode=StoreUpload /verbosity:minimal

if errorlevel 1 (
    echo.
    echo ========================================
    echo  Command-line MSIX build failed.
    echo ========================================
    echo.
    echo  This is a known issue with WinUI 3 and
    echo  command-line MSBuild. Use Visual Studio instead:
    echo.
    echo    1. Open ShutdownTimer.sln in Visual Studio
    echo    2. Right-click project in Solution Explorer
    echo    3. Select Publish ^> Create App Packages...
    echo    4. Choose "Microsoft Store" as distribution method
    echo    5. Sign in with your Partner Center account
    echo    6. Follow the wizard to generate the .msixupload
    echo.
    echo  The .msixupload file is what you submit to the Store.
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  STORE BUILD SUCCESSFUL
echo ========================================
echo.
echo  Output folder: %MSIX_OUT%\
echo.
dir /s /b "%MSIX_OUT%\*.msix" "%MSIX_OUT%\*.msixupload" 2>nul
echo.
echo  Upload the .msixupload file to Partner Center.
echo.
pause
