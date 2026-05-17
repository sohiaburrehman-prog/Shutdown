using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using ShutdownTimer.ViewModels;
using System.ComponentModel;
using System.Numerics;

namespace ShutdownTimer.Views;

public sealed partial class CountdownTimerPage : Page
{
    public CountdownTimerViewModel ViewModel { get; }
    private DispatcherTimer? _pulseTimer;

    public CountdownTimerPage()
    {
        ViewModel = App.GetService<CountdownTimerViewModel>();
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadPresets();
        SetupPulseAnimation();

        // Subscribe to ViewModel property changes to start/stop the timer
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // If the timer is already running when we navigate here, start the pulse
        if (ViewModel.IsRunning)
            _pulseTimer?.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _pulseTimer?.Stop();
        _pulseTimer = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.IsRunning)) return;

        if (ViewModel.IsRunning)
            _pulseTimer?.Start();
        else
            _pulseTimer?.Stop();
    }

    private void SetupPulseAnimation()
    {
        // Create a subtle pulsing glow effect on the timer card when running
        // Timer is NOT started here — OnViewModelPropertyChanged / OnLoaded handle that
        _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _pulseTimer.Tick += (_, _) =>
        {
            // Guard: IsRunning should always be true here, but belt-and-suspenders
            if (!ViewModel.IsRunning) return;

            var visual = ElementCompositionPreview.GetElementVisual(TimerCard);
            var compositor = visual.Compositor;

            // Subtle scale pulse
            var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnim.InsertKeyFrame(0.5f, new Vector3(1.008f, 1.008f, 1f));
            scaleAnim.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnim.Duration = TimeSpan.FromMilliseconds(1500);

            // Set center point for scaling
            visual.CenterPoint = new Vector3((float)(TimerCard.ActualWidth / 2),
                                              (float)(TimerCard.ActualHeight / 2), 0);
            visual.StartAnimation("Scale", scaleAnim);

            // Subtle opacity pulse on the timer text
            var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0f, 1f);
            opacityAnim.InsertKeyFrame(0.5f, 0.85f);
            opacityAnim.InsertKeyFrame(1f, 1f);
            opacityAnim.Duration = TimeSpan.FromMilliseconds(1500);

            var textVisual = ElementCompositionPreview.GetElementVisual(TimerDisplayText);
            textVisual.StartAnimation("Opacity", opacityAnim);
        };
        // Don't call _pulseTimer.Start() here — let OnLoaded decide based on current state
    }
}
