using System.Windows.Input;

namespace SnowblindModPlayer.Pages;

public partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SettingsScroll != null)
        {
            SettingsScroll.ScrollToVerticalOffset(SettingsScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
