using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Helpers;

public static class ThemeHelper
{
    /// <summary>
    /// Convert a theme index (0=System, 1=Light, 2=Dark) to an ElementTheme.
    /// </summary>
    public static ElementTheme IndexToElementTheme(int index) => index switch
    {
        1 => ElementTheme.Light,
        2 => ElementTheme.Dark,
        _ => ElementTheme.Default // System
    };

    /// <summary>
    /// Apply the theme to a window's root FrameworkElement and update its title bar.
    /// </summary>
    public static void ApplyTheme(Window window, int themeIndex)
    {
        var theme = IndexToElementTheme(themeIndex);

        // Apply to root content
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }

        // Update title bar to match
        UpdateTitleBar(window, theme);
    }

    /// <summary>
    /// Update the title bar colours to match the current theme.
    /// </summary>
    public static void UpdateTitleBar(Window window, ElementTheme theme)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (!AppWindowTitleBar.IsCustomizationSupported())
            return;

        var titleBar = appWindow.TitleBar;

        // Determine if we're actually in dark mode
        bool isDark = theme == ElementTheme.Dark ||
            (theme == ElementTheme.Default && IsDarkSystemTheme());

        if (isDark)
        {
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 30, 30, 30);
            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(30, 0, 0, 0);
        }
    }

    private static bool IsDarkSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
                return val == 0;
        }
        catch { }
        return true; // default to dark
    }
}
