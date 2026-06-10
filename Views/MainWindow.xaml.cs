using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShutdownTimer.Models;
using ShutdownTimer.Helpers;
using ShutdownTimer.Services;
using ShutdownTimer.ViewModels;
using ShutdownTimer.Win32;

namespace ShutdownTimer.Views;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly ISystemActionService? _actionService;
    private bool _forceShutdown;

    public MainViewModel ViewModel { get; } = App.GetService<MainViewModel>();

    public MainWindow(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        this.InitializeComponent();

        Title = "Shutdown Timer Advanced";

        // Subscribe to warning events from SystemActionService
        _actionService = App.GetService<ISystemActionService>();
        if (_actionService != null)
        {
            _actionService.WarningTick += OnWarningTick;
            _actionService.WarningCancelled += OnWarningCancelled;
        }

        // ── Mica backdrop (falls back gracefully on unsupported systems) ──
        try
        {
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Mica backdrop not supported: {ex.Message}");
        }

        // ── Window sizing & titlebar ──
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Restore saved window size and position, clamped to a visible monitor
        WindowPlacementHelper.Apply(appWindow, _settingsService.Settings.Window, persistCorrection: true, _settingsService);

        // Set window icon
        var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        if (System.IO.File.Exists(icoPath))
            appWindow.SetIcon(icoPath);

        // Custom dark title bar
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
        }

        // Apply saved theme
        var themeIndex = _settingsService.Settings.ThemeIndex;
        RootGrid.Loaded += (_, _) =>
        {
            ThemeHelper.ApplyTheme(this, themeIndex);
        };

        // Listen for theme changes from Settings page
        var settingsVm = App.GetService<SettingsViewModel>();
        settingsVm.ThemeChanged += newThemeIndex =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ThemeHelper.ApplyTheme(this, newThemeIndex);
            });
        };

        // Navigate to dashboard by default
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];

        // Intercept close to minimize to tray
        this.Closed += OnWindowClosed;

        // Intercept minimise button to hide to tray instead
        _origWndProc = WindowInterop.SetWindowLongPtr(hwnd, WindowInterop.GWL_WNDPROC,
            System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(
                _wndProcDelegate = new WindowInterop.WndProcDelegate(WndProc)));

        if (_origWndProc == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Failed to sub-class window procedure (SetWindowLongPtr returned Zero). Minimise-to-tray will not work through the standard button.");
        }
    }

    private IntPtr _origWndProc;
    private WindowInterop.WndProcDelegate _wndProcDelegate = null!;

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (_origWndProc == IntPtr.Zero)
            return IntPtr.Zero;

        if (msg == WindowInterop.WM_SIZE && (int)wParam == WindowInterop.SIZE_MINIMIZED)
        {
            HideWindow();
            return IntPtr.Zero;
        }

        return WindowInterop.CallWindowProc(_origWndProc, hWnd, msg, wParam, lParam);
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "dashboard" => typeof(DashboardPage),
                "countdown" => typeof(CountdownTimerPage),
                "process" => typeof(ProcessMonitorPage),
                "idle" => typeof(IdleDetectionPage),
                "schedule" => typeof(SchedulePage),
                "history" => typeof(HistoryPage),
                "help" => typeof(HelpPage),
                "settings" => typeof(SettingsPage),
                _ => typeof(DashboardPage)
            };

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType, null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            }
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (!_forceShutdown && _settingsService.Settings.MinimizeToTrayOnClose)
        {
            args.Handled = true;
            HideWindow();
            return;
        }

        SaveWindowState();
    }

    /// <summary>
    /// Closes the app for real, bypassing minimize-to-tray.
    /// </summary>
    public void ShutdownApplication()
    {
        _forceShutdown = true;
        SaveWindowState();
        Close();
    }

    private void SaveWindowState()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var ws = _settingsService.Settings.Window;
            ws.Width = appWindow.Size.Width;
            ws.Height = appWindow.Size.Height;
            ws.X = appWindow.Position.X;
            ws.Y = appWindow.Position.Y;
            _ = _settingsService.SaveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to save window state: {ex.Message}");
        }
    }

    public void HideWindow()
    {
        SaveWindowState();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowInterop.ShowWindow(hwnd, WindowInterop.SW_HIDE);
    }

    public void RestoreWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        WindowPlacementHelper.Apply(appWindow, _settingsService.Settings.Window, persistCorrection: true, _settingsService);

        WindowInterop.ShowWindow(hwnd, WindowInterop.SW_RESTORE);
        WindowInterop.SetForegroundWindow(hwnd);
        this.Activate();
    }

    private void OnWarningTick(int secondsLeft, TimerAction action)
    {
        WarningBanner.Visibility = Visibility.Visible;
        WarningText.Text = $"{action} in {secondsLeft} seconds — save your work!";
        RestoreWindow();
    }

    private void OnWarningCancelled()
    {
        WarningBanner.Visibility = Visibility.Collapsed;
    }

    private void WarningPostpone_Click(object sender, RoutedEventArgs e)
    {
        _actionService?.PostponeWarning(300); // +5 minutes
    }

    private void WarningPostpone10_Click(object sender, RoutedEventArgs e)
    {
        _actionService?.PostponeWarning(600); // +10 minutes
    }

    private void WarningCancel_Click(object sender, RoutedEventArgs e)
    {
        _actionService?.CancelWarning();
        WarningBanner.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Navigate to a page and update the NavigationView selection.
    /// </summary>
    public void NavigateToPage(Type pageType, string tag)
    {
        ContentFrame.Navigate(pageType);

        foreach (var menuItem in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (menuItem.Tag?.ToString() == tag)
            {
                NavView.SelectedItem = menuItem;
                return;
            }
        }
        foreach (var footerItem in NavView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            if (footerItem.Tag?.ToString() == tag)
            {
                NavView.SelectedItem = footerItem;
                return;
            }
        }
    }
}
