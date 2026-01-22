using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SnowblindModPlayer.Pages;

public partial class SettingsPage
{
    private AppSettings _settings = new();
    private MediaLibrary _library = new();

    public SettingsPage()
    {
        InitializeComponent();

        SaveBtn.Click += (_, __) => Save();

        AutostartApplyBtn.Click += async (_, __) =>
        {
            try
            {
                if (AutostartCheck.IsChecked == true)
                    TaskSchedulerService.EnableAutostart();
                else
                    TaskSchedulerService.DisableAutostart();

                await RefreshAutostartStatus();
            }
            catch (Exception ex)
            {
                AutostartStatus.Text = "Fehler: " + ex.Message;
            }
        };

        MonitorPicker.SelectionChanged += () => UpdateMonitorHint();

        Loaded += (_, __) => Reload();
    }

    private void Reload()
    {
        _settings = App.Instance.CurrentSettings;
        _library = App.Instance.CurrentLibrary;

        AutostartCheck.IsChecked = TaskSchedulerService.IsAutostartEnabled();
        _ = RefreshAutostartStatus();

        AutoPlayCheck.IsChecked = _settings.AutoPlayOnStart;
        FullscreenCheck.IsChecked = _settings.Fullscreen;
        LoopCheck.IsChecked = _settings.Loop;
        MuteCheck.IsChecked = _settings.Mute;
        VolumeSlider.Value = _settings.Volume;
        DelayBox.Value = _settings.StartDelaySeconds;

        DefaultVideoCombo.ItemsSource = _library.Items;
        DefaultVideoCombo.SelectedItem = _library.Items.FirstOrDefault(v => v.Id == _settings.DefaultVideoId);

        MonitorPicker.LoadScreens(_settings.MonitorDeviceName);
        UpdateMonitorHint();
    }

    private void Save()
    {
        _settings.AutoPlayOnStart = AutoPlayCheck.IsChecked == true;
        _settings.Fullscreen = FullscreenCheck.IsChecked == true;
        _settings.Loop = LoopCheck.IsChecked == true;
        _settings.Mute = MuteCheck.IsChecked == true;
        _settings.Volume = (int)Math.Round(VolumeSlider.Value);
        _settings.StartDelaySeconds = (int)(DelayBox.Value ?? 0);

        if (DefaultVideoCombo.SelectedItem is VideoItem item)
            _settings.DefaultVideoId = item.Id;

        _settings.MonitorDeviceName = MonitorPicker.SelectedDeviceName;

        App.Instance.SaveSettings(_settings);
        System.Windows.MessageBox.Show("Gespeichert.", "Snowblind-Mod Player", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UpdateMonitorHint()
    {
        MonitorHint.Text = string.IsNullOrWhiteSpace(MonitorPicker.SelectedDeviceName)
            ? "Bitte Monitor auswählen."
            : "Ausgewählt: " + MonitorPicker.SelectedDeviceName;
    }

    private Task RefreshAutostartStatus()
    {
        var enabled = TaskSchedulerService.IsAutostartEnabled();
        AutostartStatus.Text = enabled ? "Autostart ist aktiv." : "Autostart ist nicht aktiv.";
        return Task.CompletedTask;
    }
}
