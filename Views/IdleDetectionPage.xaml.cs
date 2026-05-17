using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.ViewModels;

namespace ShutdownTimer.Views;

public sealed partial class IdleDetectionPage : Page
{
    public IdleDetectionViewModel ViewModel { get; }

    public IdleDetectionPage()
    {
        ViewModel = App.GetService<IdleDetectionViewModel>();
        this.InitializeComponent();
    }
}
