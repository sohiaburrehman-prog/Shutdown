using ShutdownTimer.Win32;

namespace ShutdownTimer.Services;

public interface ITaskbarService
{
    void Initialize(IntPtr hwnd);
    void SetProgress(double percentage);
    void SetState(NativeMethods.TBPFLAG state);
    void Reset();
}

public class TaskbarService : ITaskbarService
{
    private readonly NativeMethods.ITaskbarList3? _taskbarList;
    private IntPtr _hwnd;

    public TaskbarService()
    {
        try
        {
            _taskbarList = (NativeMethods.ITaskbarList3)new NativeMethods.TaskbarList();
            _taskbarList.HrInit();
        }
        catch
        {
            _taskbarList = null;
        }
    }

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void SetProgress(double percentage)
    {
        if (_taskbarList == null || _hwnd == IntPtr.Zero) return;

        uint val = (uint)Math.Clamp(percentage * 100, 0, 1000); // Using 1000 for precision
        _taskbarList.SetProgressValue(_hwnd, val, 1000);
    }

    public void SetState(NativeMethods.TBPFLAG state)
    {
        if (_taskbarList == null || _hwnd == IntPtr.Zero) return;
        _taskbarList.SetProgressState(_hwnd, state);
    }

    public void Reset()
    {
        SetState(NativeMethods.TBPFLAG.TBPF_NOPROGRESS);
    }
}
