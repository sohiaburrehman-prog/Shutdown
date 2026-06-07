using System.Drawing;
using System.Drawing.Drawing2D;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Media;
using ShutdownTimer.Win32;

namespace ShutdownTimer.Helpers;

/// <summary>
/// Builds a high-contrast tray icon that stays visible on the Windows 11 taskbar.
/// </summary>
public static class TrayIconHelper
{
    private static readonly Color Background = Color.FromArgb(255, 15, 15, 26);
    private static readonly Color Cyan = Color.FromArgb(255, 0, 212, 255);
    private static readonly Color CyanDim = Color.FromArgb(255, 0, 96, 128);

    public static ImageSource CreateFallbackIconSource()
    {
        return new GeneratedIconSource
        {
            Text = "\uE7E8",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = 28,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 0, 212, 255)),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 255, 255, 255)),
        };
    }

    public static Icon CreateNativeTrayIcon()
    {
        using var bitmap = RenderTrayBitmap(32);
        var handle = bitmap.GetHicon();
        try
        {
            using var source = Icon.FromHandle(handle);
            return (Icon)source.Clone();
        }
        finally
        {
            WindowInterop.DestroyIcon(handle);
        }
    }

    public static bool TryApplyNativeIcon(TaskbarIcon trayIcon)
    {
        try
        {
            using var icon = CreateNativeTrayIcon();
            trayIcon.UpdateIcon((Icon)icon.Clone());
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayIconHelper] Native icon failed: {ex.Message}");
            return false;
        }
    }

    public static bool TryApplyFileIcon(TaskbarIcon trayIcon, string icoPath)
    {
        if (!File.Exists(icoPath))
            return false;

        try
        {
            using var icon = new Icon(icoPath);
            trayIcon.UpdateIcon((Icon)icon.Clone());
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayIconHelper] Failed to load {icoPath}: {ex.Message}");
            return false;
        }
    }

    public static string GetTrayIcoPath()
        => Path.Combine(AppContext.BaseDirectory, "Resources", "TrayIcons", "tray.ico");

    public static string GetExecutablePath()
        => Environment.ProcessPath
           ?? Path.Combine(AppContext.BaseDirectory, "ShutdownTimer.exe");

    private static Bitmap RenderTrayBitmap(int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var cx = size / 2f;
        var cy = size / 2f;
        var radius = size * 0.34f;
        var stroke = Math.Max(2f, size * 0.11f);
        var thin = Math.Max(1f, size * 0.05f);

        using var bgBrush = new SolidBrush(Background);
        graphics.FillEllipse(bgBrush, cx - radius - 1, cy - radius - 1, (radius + 1) * 2, (radius + 1) * 2);

        using var cyanPen = new Pen(Cyan, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var dimPen = new Pen(CyanDim, thin) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        graphics.DrawArc(dimPen, cx - radius, cy - radius, radius * 2, radius * 2, 200, 140);
        graphics.DrawArc(cyanPen, cx - radius, cy - radius, radius * 2, radius * 2, 285, 70);
        graphics.DrawArc(cyanPen, cx - radius, cy - radius, radius * 2, radius * 2, 42, 276);
        graphics.DrawLine(cyanPen, cx, cy - radius * 0.62f, cx, cy - radius * 0.08f);

        return bitmap;
    }
}
