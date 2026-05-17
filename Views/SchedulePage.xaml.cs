using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

public sealed partial class SchedulePage : Page
{
    public ScheduleViewModel ViewModel { get; }

    public SchedulePage()
    {
        ViewModel = App.GetService<ScheduleViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.LoadSchedules();
    }
}
