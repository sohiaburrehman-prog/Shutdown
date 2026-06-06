using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
                SetupTrayIcon();
                trayReady = _trayIcon != null;
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

            if (settingsService.Settings.StartMinimized && trayReady)
            {
                // Defer hide until after WinUI finishes the initial show/activate pass.
                _mainWindow.DispatcherQueue.TryEnqueue(() => _mainWindow.HideWindow());
            }
            else
            {
                _mainWindow.RestoreWindow();
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

    private void SetupTrayIcon()
    {
        var exeDir = AppContext.BaseDirectory;
        var trayDir = System.IO.Path.Combine(exeDir, "Resources", "TrayIcons");
        var trayIcoPath = System.IO.Path.Combine(trayDir, "tray.ico");
        var trayPngPath = System.IO.Path.Combine(trayDir, "tray.png");

        Microsoft.UI.Xaml.Media.ImageSource trayIconSource;
        if (System.IO.File.Exists(trayIcoPath))
        {
            trayIconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri(System.IO.Path.GetFullPath(trayIcoPath)));
        }
        else if (System.IO.File.Exists(trayPngPath))
        {
            trayIconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri(System.IO.Path.GetFullPath(trayPngPath)));
        }
        else
        {
            trayIconSource = new H.NotifyIcon.GeneratedIconSource
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 0, 212, 255)),
                Text = "\uE7E8"
            };
        }

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Shutdown Timer Advanced",
            IconSource = trayIconSource
        };
        _trayIcon.ForceCreate();

        var menu = new MenuFlyout();

        var showItem = new MenuFlyoutItem { Text = "Show Window" };
        showItem.Click += (_, _) => _mainWindow.RestoreWindow();
        menu.Items.Add(showItem);

        // Mini mode option
        var miniItem = new MenuFlyoutItem { Text = "Mini Timer" };
        miniItem.Click += (_, _) => ShowMiniWindow();
        menu.Items.Add(miniItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        // Quick actions (including Lock)
        foreach (var action in Enum.GetValues<TimerAction>())
        {
            var item = new MenuFlyoutItem { Text = $"Quick {action}" };
            var capturedAction = action;
            item.Click += (_, _) =>
            {
                var svc = GetService<ISystemActionService>();
                svc.ExecuteWithWarning(capturedAction, "Tray Quick Action", $"Tray: Quick {capturedAction}");
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) =>
        {
            _trayIcon?.Dispose();
            _miniWindow?.Close();
            _mainWindow.Close();
            Environment.Exit(0);
        };
        menu.Items.Add(exitItem);

        _trayIcon.ContextFlyout = menu;
        _trayIcon.LeftClickCommand = new SimpleRelayCommand(() => _mainWindow.RestoreWindow());
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
