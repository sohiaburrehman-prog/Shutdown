using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ShutdownTimer.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace ShutdownTimer.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }

    private async void PrivacyLink_Click(object sender, RoutedEventArgs e)
    {
        var privacyPath = Path.Combine(AppContext.BaseDirectory, "PRIVACY.md");
        if (File.Exists(privacyPath))
        {
            Process.Start(new ProcessStartInfo(privacyPath) { UseShellExecute = true });
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "Privacy Policy",
                Content = "This app does not collect, transmit, or store any personal data. " +
                          "All settings are stored locally on your device. " +
                          "No analytics, telemetry, or usage data is sent anywhere.\n\n" +
                          "Developer: Sohiab\nContact: sohiab@outlook.com",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void EulaLink_Click(object sender, RoutedEventArgs e)
    {
        var eulaPath = Path.Combine(AppContext.BaseDirectory, "EULA.md");
        if (File.Exists(eulaPath))
        {
            Process.Start(new ProcessStartInfo(eulaPath) { UseShellExecute = true });
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "End-User License Agreement",
                Content = "Shutdown Timer Advanced is provided \"as is\" without warranty of any kind. " +
                          "The software performs system actions (shutdown, restart, sleep, hibernate, log off) " +
                          "which may result in loss of unsaved work. The user is responsible for saving all work " +
                          "before initiating any action.\n\n" +
                          "This software does not collect any personal data. " +
                          "See the full EULA document for complete terms.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
