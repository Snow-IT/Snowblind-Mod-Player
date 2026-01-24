using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnowblindModPlayer.Pages;

public partial class SettingsPage : Page
{
    private AppSettings _settings = new();
    private MediaLibrary _library = new();
    private bool _isInitializing = false;

    public SettingsPage()
    {
        InitializeComponent();

        if (AutostartApplyBtn != null)
        {
            AutostartApplyBtn.Click += async (_, __) =>
            {
                try
                {
                    if (AutostartCheck?.IsChecked == true)
                        TaskSchedulerService.EnableAutostart();
                    else
                        TaskSchedulerService.DisableAutostart();

                    await RefreshAutostartStatus();
                }
                catch (Exception ex)
                {
                    if (AutostartStatus != null) AutostartStatus.Text = "Fehler: " + ex.Message;
                }
            };
        }

        if (MonitorPicker != null)
            MonitorPicker.SelectionChanged += () => { UpdateMonitorHint(); ApplySettingsFromControls(); };

        if (AutoPlayCheck != null) { AutoPlayCheck.Checked += (_, __) => ApplySettingsFromControls(); AutoPlayCheck.Unchecked += (_, __) => ApplySettingsFromControls(); }
        if (FullscreenCheck != null) { FullscreenCheck.Checked += (_, __) => ApplySettingsFromControls(); FullscreenCheck.Unchecked += (_, __) => ApplySettingsFromControls(); }
        if (LoopCheck != null) { LoopCheck.Checked += (_, __) => ApplySettingsFromControls(); LoopCheck.Unchecked += (_, __) => ApplySettingsFromControls(); }
        if (MuteCheck != null) { MuteCheck.Checked += (_, __) => ApplySettingsFromControls(); MuteCheck.Unchecked += (_, __) => ApplySettingsFromControls(); }
        if (AdvancedLoggingCheck != null) { AdvancedLoggingCheck.Checked += (_, __) => ApplySettingsFromControls(); AdvancedLoggingCheck.Unchecked += (_, __) => ApplySettingsFromControls(); }

        if (VolumeSlider != null) VolumeSlider.ValueChanged += (_, __) => ApplySettingsFromControls();
        if (DelayBox != null) DelayBox.ValueChanged += (_, __) => ApplySettingsFromControls();

        if (DefaultVideoCombo != null) DefaultVideoCombo.SelectionChanged += (_, __) => ApplySettingsFromControls();

        Loaded += (_, __) => Reload();
    }

    private void Reload()
    {
        _isInitializing = true;

        _settings = App.Instance.CurrentSettings;
        _library = App.Instance.CurrentLibrary;

        if (AutostartCheck != null) AutostartCheck.IsChecked = TaskSchedulerService.IsAutostartEnabled();
        _ = RefreshAutostartStatus();

        if (AutoPlayCheck != null) AutoPlayCheck.IsChecked = _settings.AutoPlayOnStart;
        if (FullscreenCheck != null) FullscreenCheck.IsChecked = _settings.Fullscreen;
        if (LoopCheck != null) LoopCheck.IsChecked = _settings.Loop;
        if (MuteCheck != null) MuteCheck.IsChecked = _settings.Mute;
        if (VolumeSlider != null) VolumeSlider.Value = _settings.Volume;
        if (DelayBox != null) DelayBox.Value = _settings.StartDelaySeconds;

        if (AdvancedLoggingCheck != null) AdvancedLoggingCheck.IsChecked = _settings.AdvancedLogging;

        if (DefaultVideoCombo != null)
        {
            DefaultVideoCombo.ItemsSource = _library.Items;
            DefaultVideoCombo.SelectedItem = _library.Items.FirstOrDefault(v => v.Id == _settings.DefaultVideoId);
        }

        if (MonitorPicker != null) MonitorPicker.LoadScreens(_settings.MonitorDeviceName);
        UpdateMonitorHint();

        _isInitializing = false;
    }

    private void ApplySettingsFromControls()
    {
        if (_isInitializing) return;

        _settings.AutoPlayOnStart = AutoPlayCheck?.IsChecked == true;
        _settings.Fullscreen = FullscreenCheck?.IsChecked == true;
        _settings.Loop = LoopCheck?.IsChecked == true;
        _settings.Mute = MuteCheck?.IsChecked == true;
        _settings.Volume = (int)Math.Round(VolumeSlider?.Value ?? _settings.Volume);
        _settings.StartDelaySeconds = (int)(DelayBox?.Value ?? _settings.StartDelaySeconds);
        _settings.AdvancedLogging = AdvancedLoggingCheck?.IsChecked == true;
        _settings.MonitorDeviceName = MonitorPicker?.SelectedDeviceName ?? _settings.MonitorDeviceName;

        if (DefaultVideoCombo?.SelectedItem is VideoItem item)
            _settings.DefaultVideoId = item.Id;

        App.Instance.SaveSettings(_settings);

        try { _ = DialogService.ToastAsync("Einstellungen übernommen"); } catch { }
        try { LogService.LogInfo("Settings changed via UI. AutoPlay=" + _settings.AutoPlayOnStart + ", AdvancedLogging=" + _settings.AdvancedLogging); } catch { }
    }

    private void UpdateMonitorHint()
    {
        if (MonitorHint == null || MonitorPicker == null) return;
        MonitorHint.Text = string.IsNullOrWhiteSpace(MonitorPicker.SelectedDeviceName)
            ? "Bitte Monitor auswählen."
            : "Ausgewählt: " + MonitorPicker.SelectedDeviceName;
    }

    private Task RefreshAutostartStatus()
    {
        var enabled = TaskSchedulerService.IsAutostartEnabled();
        if (AutostartStatus != null) AutostartStatus.Text = enabled ? "Autostart ist aktiv." : "Autostart ist nicht aktiv.";
        return Task.CompletedTask;
    }
}