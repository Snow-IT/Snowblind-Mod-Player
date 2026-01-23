using System.Windows;

namespace SnowblindModPlayer;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DialogService.Initialize(RootDialogHost);
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        Nav.Navigate(typeof(Pages.VideosPage));
    }

    public void SetHint(string message)
    {
        HintBar.Message = message;
        HintBar.IsOpen = true;
    }

    public void RefreshBindings()
    {
        // Force pages to refresh bindings by recreating their DataContext on next navigation.
        // (MVP approach) The pages themselves bind to App.Instance state on Loaded.
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Close button -> to tray
        e.Cancel = true;
        Hide();
    }
}
