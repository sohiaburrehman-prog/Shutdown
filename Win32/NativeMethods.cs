using System.Runtime.InteropServices;

namespace ShutdownTimer.Win32;

public static class NativeMethods
{
    // ── Idle detection ──────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // Environment.TickCount (32-bit) wraps at ~49 days just like dwTime,
        // so the subtraction is correct when both overflow in sync.
        var idleMs = (uint)(Environment.TickCount - (int)info.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }

    // ── Power management ────────────────────────────────────────────
    [DllImport("PowrProf.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    public const uint EWX_LOGOFF = 0x00;

    // ── Lock workstation ────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool LockWorkStation();

    [DllImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern bool MessageBeep(uint uType);

    // ── Taskbar Progress (ITaskbarList3) ────────────────────────────
    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITaskbarList3
    {
        // ITaskbarList
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActivateAlt(IntPtr hwnd);

        // ITaskbarList2
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        void UnregisterTab(IntPtr hwndTab);
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButton); // Simplified
        void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButton);
        void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
        void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
        void SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
    }

    [ComImport]
    [Guid("56308069-2c4c-11d1-8b21-00a0c9971e04")]
    [ClassInterface(ClassInterfaceType.None)]
    public class TaskbarList { }

    public enum TBPFLAG : uint
    {
        TBPF_NOPROGRESS = 0,
        TBPF_INDETERMINATE = 1,
        TBPF_NORMAL = 2,
        TBPF_ERROR = 4,
        TBPF_PAUSED = 8
    }
}
