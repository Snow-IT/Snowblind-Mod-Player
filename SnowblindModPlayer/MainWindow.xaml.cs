using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace SnowblindModPlayer;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        // Default page
        Nav.Navigate(typeof(Pages.VideosPage));
    }

    public void SetHint(string message)
    {
        HintBar.Message = message;
        HintBar.IsOpen = true;
    }

    public void RefreshFromAppState()
    {
        // Force refresh of current page by navigating to itself
        // This is a simple MVP approach.
        // If you prefer, we can implement INavigationAware in pages and refresh there.
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // X button -> minimize to tray
        e.Cancel = true;
        Hide();
    }
}
