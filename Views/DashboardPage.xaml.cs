using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using ShutdownTimer.Models;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        this.InitializeComponent();

        ViewModel.ConfirmQuickAction += OnConfirmQuickAction;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Refresh();
    }

    private async void OnConfirmQuickAction(TimerAction action)
    {
        var dialog = new ContentDialog
        {
            Title = $"Confirm {action}",
            Content = $"{action} will start the same warning countdown used by timers, schedules, process monitors, and tray actions. You can still cancel or postpone before the final system action.",
            PrimaryButtonText = $"Start {action} warning",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.ExecuteQuickAction(action);
        }
    }

    // ── Clickable status cards → navigate to relevant page ──
    private void CountdownButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("countdown");
    }

    private void CountdownCard_Click(object sender, PointerRoutedEventArgs e)
    {
        NavigateToPage("countdown");
    }

    private void ProcessCard_Click(object sender, PointerRoutedEventArgs e)
    {
        NavigateToPage("process");
    }

    private void IdleCard_Click(object sender, PointerRoutedEventArgs e)
    {
        NavigateToPage("idle");
    }

    private void ScheduleCard_Click(object sender, PointerRoutedEventArgs e)
    {
        NavigateToPage("schedule");
    }

    private void ScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("schedule");
    }

    private void HistoryCard_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("history");
    }

    private void NavigateToPage(string tag)
    {
        // Navigate via the MainWindow's NavigationView
        if (App.MainWindow is MainWindow mainWindow)
        {
            var pageType = tag switch
            {
                "countdown" => typeof(CountdownTimerPage),
                "process" => typeof(ProcessMonitorPage),
                "idle" => typeof(IdleDetectionPage),
                "schedule" => typeof(SchedulePage),
                "history" => typeof(HistoryPage),
                _ => typeof(DashboardPage)
            };

            // Navigate and update NavView selection
            mainWindow.NavigateToPage(pageType, tag);
        }
    }
}
