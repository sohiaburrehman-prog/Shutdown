using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.Helpers;

/// <summary>
/// Keeps the main window on a visible monitor work area (fixes off-screen coords after display changes).
/// </summary>
public static class WindowPlacementHelper
{
    private const int MinWidth = 700;
    private const int MinHeight = 550;
    private const int DefaultWidth = 900;
    private const int DefaultHeight = 620;

    public static void Apply(AppWindow appWindow, WindowState windowState, bool persistCorrection, ISettingsService? settingsService = null)
    {
        var (width, height, x, y, corrected) = Clamp(windowState, appWindow.Id);

        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        appWindow.Move(new Windows.Graphics.PointInt32(x, y));

        if (corrected && persistCorrection && settingsService != null)
        {
            windowState.Width = width;
            windowState.Height = height;
            windowState.X = x;
            windowState.Y = y;
            _ = settingsService.SaveAsync();
        }
    }

    public static (int Width, int Height, int X, int Y, bool Corrected) Clamp(WindowState windowState, WindowId windowId)
    {
        var width = windowState.Width > MinWidth ? (int)windowState.Width : DefaultWidth;
        var height = windowState.Height > MinHeight ? (int)windowState.Height : DefaultHeight;

        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var work = displayArea.WorkArea;

        var x = (int)windowState.X;
        var y = (int)windowState.Y;
        var corrected = false;

        if (!IsVisibleOnWorkArea(work, width, height, x, y))
        {
            x = work.X + Math.Max(0, (work.Width - width) / 2);
            y = work.Y + Math.Max(0, (work.Height - height) / 2);
            corrected = true;
        }
        else
        {
            var maxX = work.X + Math.Max(0, work.Width - width);
            var maxY = work.Y + Math.Max(0, work.Height - height);
            var clampedX = Math.Clamp(x, work.X, maxX);
            var clampedY = Math.Clamp(y, work.Y, maxY);
            if (clampedX != x || clampedY != y)
            {
                x = clampedX;
                y = clampedY;
                corrected = true;
            }
        }

        return (width, height, x, y, corrected);
    }

    private static bool IsVisibleOnWorkArea(RectInt32 work, int width, int height, int x, int y)
    {
        // Reject absurd coordinates saved before clamping (e.g. broken multi-monitor state).
        if (x > 10_000 || y > 10_000 || x < -width || y < -height)
            return false;

        var windowRect = new RectInt32(x, y, width, height);
        return Intersects(work, windowRect);
    }

    private static bool Intersects(RectInt32 a, RectInt32 b)
    {
        return a.X < b.X + b.Width
            && a.X + a.Width > b.X
            && a.Y < b.Y + b.Height
            && a.Y + a.Height > b.Y;
    }
}
