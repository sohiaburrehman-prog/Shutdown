using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ShutdownTimer.Services;

namespace ShutdownTimer.Views;

/// <summary>
/// A compact always-on-top floating window that shows the active countdown timer.
/// </summary>
public sealed partial class MiniWindow : Window
{
    private readonly ITimerService _timerService;

    public MiniWindow()
    {
        this.InitializeComponent();

        Title = "Timer";

        _timerService = App.GetService<ITimerService>();
        _timerService.Tick += OnTick;
        _timerService.Completed += OnCompleted;

        // Unsubscribe on close regardless of how the window is closed
        this.Closed += (_, _) =>
        {
            _timerService.Tick -= OnTick;
            _timerService.Completed -= OnCompleted;
        };

        // Configure as small always-on-top window
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Small pill-like size
        appWindow.Resize(new Windows.Graphics.SizeInt32(260, 60));

        // Always on top
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // Custom transparent title bar
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Transparent;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        }

        // Set initial timer display
        UpdateTimerDisplay();
    }

    private void OnTick(TimeSpan remaining)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TimerText.Text = remaining.ToString(@"hh\:mm\:ss");
        });
    }

    private void OnCompleted()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TimerText.Text = "Done!";
        });
    }

    private void UpdateTimerDisplay()
    {
        var state = _timerService.State;
        if (state == Models.TimerState.Running || state == Models.TimerState.Paused)
        {
            TimerText.Text = _timerService.Remaining.ToString(@"hh\:mm\:ss");
        }
        else
        {
            TimerText.Text = "--:--:--";
        }
    }

    private void ExpandBtn_Click(object sender, RoutedEventArgs e)
    {
        // Show main window and close mini
        App.MainWindow?.RestoreWindow();
        this.Close();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        this.Close(); // Closed event handler takes care of unsubscribing
    }
}
