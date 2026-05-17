using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        ViewModel = App.GetService<HistoryViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }
}
