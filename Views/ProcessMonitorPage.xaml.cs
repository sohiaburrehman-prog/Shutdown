using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

public sealed partial class ProcessMonitorPage : Page
{
    public ProcessMonitorViewModel ViewModel { get; }

    public ProcessMonitorPage()
    {
        ViewModel = App.GetService<ProcessMonitorViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshProcessesCommand.Execute(null);
    }
}
