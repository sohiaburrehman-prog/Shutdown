using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ShutdownTimer.Helpers;
using ShutdownTimer.Models;
using ShutdownTimer.Services;
using ShutdownTimer.ViewModels;
using ShutdownTimer.Views;
using ShutdownTimer.Win32;

namespace ShutdownTimer;

public partial class App : Application
{
    private static readonly Mutex SingleInstanceMutex;
    private static readonly bool IsFirstInstance;

    static App()
    {
        SingleInstanceMutex = new Mutex(true, "ShutdownTimerAdvanced_SingleInstance", out bool createdNew);
        IsFirstInstance = createdNew;
    }

    private static IServiceProvider _services = null!;
    private MainWindow _mainWindow = null!;
    private TaskbarIcon? _trayIcon;
    private MiniWindow? _miniWindow;
    private bool _trayMenuAttached;

    /// <summary>
    /// The main window instance, accessible for cross-page navigation.
    /// </summary>
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        if (!IsFirstInstance)
        {
            TryActivateExistingInstance();
            Environment.Exit(0);
            return;
        }

        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!IsFirstInstance)
            return;

        try
        {
            // ── Build DI container ──────────────────────────────
            var services = new ServiceCollection();

            // Services (singletons — shared across VMs)
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ISystemActionService, SystemActionService>();
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<IProcessMonitorService, ProcessMonitorService>();
            services.AddSingleton<IIdleDetectionService, IdleDetectionService>();
            services.AddSingleton<IScheduleService, ScheduleService>();
            services.AddSingleton<ITaskbarService, TaskbarService>();
            services.AddSingleton<IPowerService, PowerService>();
            services.AddSingleton<IBatteryAutomationService, BatteryAutomationService>();

            // ViewModels
            services.AddSingleton<CountdownTimerViewModel>();
            services.AddSingleton<ProcessMonitorViewModel>();
            services.AddSingleton<IdleDetectionViewModel>();
            services.AddSingleton<ScheduleViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<MainViewModel>();

            _services = services.BuildServiceProvider();

            // ── Load persisted settings ─────────────────────────
            var settingsService = GetService<ISettingsService>();
            await settingsService.LoadAsync();

            _mainWindow = new MainWindow(settingsService);
            MainWindow = _mainWindow;
            _mainWindow.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
            GetService<ITaskbarService>().Initialize(hwnd);

            _ = SetupJumpListAsync();

            // ── System tray ─────────────────────────────────────
            var trayReady = false;
            try
            {
                trayReady = EnsureTrayIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Tray icon failed: {ex.Message}");
            }

            // ── Start schedule service ──────────────────────────
            var scheduleService = GetService<IScheduleService>();
            scheduleService.Start(settingsService);

            GetService<IBatteryAutomationService>().Start();

            // ── Hook timer for tray tooltip update ──────────────
            var timerService = GetService<ITimerService>();
            timerService.Tick += remaining =>
            {
                if (_trayIcon != null)
                {
                    try { _trayIcon.ToolTipText = $"Shutdown Timer Advanced - {remaining:hh\\:mm\\:ss}"; }
                    catch { /* Ignore if tray was disposed */ }
                }
            };

            if (settingsService.Settings.StartMinimized)
            {
                if (trayReady)
                {
                    _mainWindow.DispatcherQueue.TryEnqueue(async () =>
                        await CompleteTrayStartupAsync(startMinimized: true));
                }
                else
                {
                    _mainWindow.RestoreWindow();
                    ScheduleTrayRetry(hideWhenReady: true);
                }
            }
            else
            {
                _mainWindow.RestoreWindow();
                if (trayReady)
                    _ = TrayVisibilityHelper.EnsurePromotedAsync(TrayIconHelper.GetExecutablePath(), TimeSpan.FromSeconds(4));
                else
                    ScheduleTrayRetry(hideWhenReady: false);
            }

            ProcessActivationArguments(args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] OnLaunched failed: {ex}");
            LogCrash(ex);
            throw;
        }
    }

    private void ProcessActivationArguments(LaunchActivatedEventArgs args)
    {
        // Handle Toast Activation
        var toastArgs = args.Arguments;
        if (string.IsNullOrWhiteSpace(toastArgs)) return;

        var parts = toastArgs.Split('&')
            .Select(p => p.Split('='))
            .ToDictionary(a => a[0], a => a.Length > 1 ? a[1] : "");

        if (parts.ContainsKey("action"))
        {
            var action = parts["action"];
            var actionService = GetService<ISystemActionService>();

            if (action == "cancel")
            {
                actionService.CancelWarning();
                GetService<ITimerService>().Cancel();
            }
            else if (action == "postpone" && parts.ContainsKey("minutes"))
            {
                if (int.TryParse(parts["minutes"], out int mins))
                {
                    actionService.CancelWarning();
                    var timer = GetService<ITimerService>();
                    var current = timer.Remaining;
                    timer.Start(current + TimeSpan.FromMinutes(mins));
                }
            }
            else if (action == "preset" && parts.TryGetValue("minutes", out var presetMinutes)
                     && int.TryParse(presetMinutes, out int jumpMinutes) && jumpMinutes > 0)
            {
                var countdown = GetService<CountdownTimerViewModel>();
                countdown.StartQuickCountdown(jumpMinutes);
                MainWindow?.RestoreWindow();
            }
        }
    }

    private async Task SetupJumpListAsync()
    {
        if (!Windows.UI.StartScreen.JumpList.IsSupported()) return;

        var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
        jumpList.Items.Clear();

        var presets = new[] { 15, 30, 60 };
        foreach (var p in presets)
        {
            var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments($"action=preset&minutes={p}", $"Shutdown in {p} minutes");
            item.Logo = new Uri("ms-appx:///Assets/StoreLogo.png"); // Use app icon
            jumpList.Items.Add(item);
        }

        await jumpList.SaveAsync();
    }

    private bool EnsureTrayIcon()
    {
        if (_trayIcon?.IsCreated == true)
            return true;

        _trayIcon?.Dispose();
        _trayMenuAttached = false;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Shutdown Timer Advanced",
            IconSource = TrayIconHelper.CreateFallbackIconSource(),
        };

        _trayIcon.ForceCreate(enablesEfficiencyMode: false);

        if (!_trayIcon.IsCreated)
            return false;

        if (!TrayIconHelper.TryApplyNativeIcon(_trayIcon))
            TrayIconHelper.TryApplyFileIcon(_trayIcon, TrayIconHelper.GetTrayIcoPath());

        _trayIcon.TrayIcon?.UpdateVisibility(IconVisibility.Visible);

        AttachTrayMenu();
        return true;
    }

    private async Task CompleteTrayStartupAsync(bool startMinimized)
    {
        var exePath = TrayIconHelper.GetExecutablePath();
        var promoted = await TrayVisibilityHelper.EnsurePromotedAsync(exePath, TimeSpan.FromSeconds(8));

        if (startMinimized && promoted)
        {
            _mainWindow.HideWindow();
            NotifyTrayPresence();
            return;
        }

        if (startMinimized)
        {
            _mainWindow.RestoreWindow();
            await ShowTraySetupDialogAsync();
        }
    }

    private void AttachTrayMenu()
    {
        if (_trayIcon == null || _trayMenuAttached)
            return;

        var menu = new MenuFlyout();

        menu.Items.Add(CreateTrayMenuItem("Show Window", () => _mainWindow.RestoreWindow()));
        menu.Items.Add(CreateTrayMenuItem("Mini Timer", ShowMiniWindow));

        menu.Items.Add(new MenuFlyoutSeparator());

        foreach (var action in Enum.GetValues<TimerAction>())
        {
            var capturedAction = action;
            menu.Items.Add(CreateTrayMenuItem($"Quick {action}", () =>
            {
                var svc = GetService<ISystemActionService>();
                svc.ExecuteWithWarning(capturedAction, "Tray Quick Action", $"Tray: Quick {capturedAction}");
            }));
        }

        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(CreateTrayMenuItem("Exit", ExitApplication));

        _trayIcon.ContextFlyout = menu;
        _trayIcon.LeftClickCommand = new SimpleRelayCommand(() => _mainWindow.RestoreWindow());
        _trayMenuAttached = true;
    }

    /// <summary>
    /// H.NotifyIcon builds a Win32 popup menu that invokes MenuFlyoutItem.Command, not Click.
    /// </summary>
    private MenuFlyoutItem CreateTrayMenuItem(string text, Action action)
    {
        var command = new XamlUICommand();
        command.ExecuteRequested += (_, _) =>
            _mainWindow.DispatcherQueue.TryEnqueue(() => action());

        return new MenuFlyoutItem
        {
            Text = text,
            Command = command,
        };
    }

    private void ExitApplication()
    {
        try
        {
            GetService<IScheduleService>().Stop();
            GetService<IBatteryAutomationService>().Stop();
            GetService<IProcessMonitorService>().StopMonitoring();
            GetService<IIdleDetectionService>().StopMonitoring();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Shutdown service stop failed: {ex.Message}");
        }

        try
        {
            _miniWindow?.Close();
            _miniWindow = null;
        }
        catch { }

        _trayIcon?.Dispose();
        _trayIcon = null;

        _mainWindow.ShutdownApplication();

        // WinUI may keep running after Close when minimize-to-tray was enabled.
        Environment.Exit(0);
    }

    private void ScheduleTrayRetry(bool hideWhenReady)
    {
        var attempts = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            attempts++;
            if (attempts > 5)
            {
                timer.Stop();
                return;
            }

            if (!EnsureTrayIcon())
                return;

            timer.Stop();
            if (hideWhenReady)
                _ = CompleteTrayStartupAsync(startMinimized: true);
        };
        timer.Start();
    }

    private async Task ShowTraySetupDialogAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Turn on the tray icon",
                Content =
                    "Windows 11 hides new tray icons until you enable them.\n\n" +
                    "Open Settings → Personalization → Taskbar → Other system tray icons, " +
                    "then switch Shutdown Timer Advanced to On.\n\n" +
                    "The app will keep running either way — use the window below until the icon appears.",
                PrimaryButtonText = "Open Taskbar settings",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _mainWindow.Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:taskbar")
                {
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Tray setup dialog failed: {ex.Message}");
        }
    }

    private void NotifyTrayPresence()
    {
        try
        {
            _trayIcon?.ShowNotification(
                "Shutdown Timer Advanced",
                "Running in the notification area near the clock. Left-click the cyan power icon to open.",
                NotificationIcon.Info);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Tray notification failed: {ex.Message}");
        }
    }

    private void ShowMiniWindow()
    {
        try
        {
            _miniWindow?.Close();
        }
        catch { }

        _miniWindow = new MiniWindow();
        _miniWindow.Activate();
    }

    public static T GetService<T>() where T : notnull
        => _services.GetRequiredService<T>();

    private static void TryActivateExistingInstance()
    {
        try
        {
            var hwnd = WindowInterop.FindWindow(null, "Shutdown Timer Advanced");
            if (hwnd == IntPtr.Zero)
                return;

            WindowInterop.ShowWindow(hwnd, WindowInterop.SW_RESTORE);
            WindowInterop.SetForegroundWindow(hwnd);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to activate existing instance: {ex.Message}");
        }
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            System.IO.File.WriteAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH\n{ex}\n");
        }
        catch { }
    }

    private class SimpleRelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;
        public SimpleRelayCommand(Action execute) => _execute = execute;
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }
}
