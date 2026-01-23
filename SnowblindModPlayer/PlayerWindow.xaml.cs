using LibVLCSharp.Shared;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnowblindModPlayer;

public partial class PlayerWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private Media? _media;

    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _osdHideTimer;

    private bool _isFullscreen;

    public PlayerWindow()
    {
        InitializeComponent();
        Core.Initialize();

        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _player;

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _uiTimer.Tick += (_, __) => UpdateOsd();

        _osdHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _osdHideTimer.Tick += (_, __) => { Osd.Visibility = Visibility.Collapsed; _osdHideTimer.Stop(); };

        Hide();
    }

    public void Play(AppSettings settings, string path)
    {
        PlaceFullscreen(settings.Fullscreen);
        ApplyPlayerSettings(settings);

        _media?.Dispose();
        _media = new Media(_libVlc, new Uri(path));
        _player.Play(_media);

        _uiTimer.Start();
        Show();
        Activate();
        Focus();
        Keyboard.Focus(this);

        ShowOsd($"Playing: {System.IO.Path.GetFileName(path)}");
    }

    public void Stop()
    {
        _player.Stop();
        _uiTimer.Stop();
        Hide();
    }

    private void ApplyPlayerSettings(AppSettings s)
    {
        _player.Mute = s.Mute;
        if (!s.Mute)
            _player.Volume = Math.Clamp(s.Volume, 0, 100);
    }

    private void PlaceFullscreen(bool enable)
    {
        _isFullscreen = enable;

        WindowState = WindowState.Normal;

        if (enable)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            ShowInTaskbar = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TogglePause();
        ShowOsd(_player.IsPlaying ? "Playing" : "Paused");
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ShowOsd(null);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                TogglePause();
                ShowOsd(_player.IsPlaying ? "Playing" : "Paused");
                e.Handled = true;
                break;

            case Key.Left:
                SeekBy(-5);
                ShowOsd("Seek -5s");
                e.Handled = true;
                break;

            case Key.Right:
                SeekBy(5);
                ShowOsd("Seek +5s");
                e.Handled = true;
                break;

            case Key.Up:
            case Key.Add:
            case Key.OemPlus:
                ChangeVolume(5);
                ShowOsd(_player.Mute ? "Muted" : $"Vol {_player.Volume}");
                e.Handled = true;
                break;

            case Key.Down:
            case Key.Subtract:
            case Key.OemMinus:
                ChangeVolume(-5);
                ShowOsd(_player.Mute ? "Muted" : $"Vol {_player.Volume}");
                e.Handled = true;
                break;

            case Key.M:
                _player.Mute = !_player.Mute;
                ShowOsd(_player.Mute ? "Muted" : $"Vol {_player.Volume}");
                e.Handled = true;
                break;

            case Key.F11:
                PlaceFullscreen(!_isFullscreen);
                ShowOsd(_isFullscreen ? "Fullscreen" : "Windowed");
                e.Handled = true;
                break;

            case Key.Escape:
                if (_isFullscreen)
                {
                    PlaceFullscreen(false);
                    ShowOsd("Windowed");
                }
                else
                {
                    Stop();
                }
                e.Handled = true;
                break;
        }
    }

    private void TogglePause() => _player.Pause();

    private void SeekBy(int seconds)
    {
        var newTime = _player.Time + (seconds * 1000L);
        if (newTime < 0) newTime = 0;
        if (_player.Length > 0 && newTime > _player.Length) newTime = _player.Length;
        _player.Time = newTime;
    }

    private void ChangeVolume(int delta)
    {
        _player.Mute = false;
        _player.Volume = Math.Clamp(_player.Volume + delta, 0, 100);
    }

    private void ShowOsd(string? status)
    {
        if (status != null)
            OsdLine1.Text = status;

        Osd.Visibility = Visibility.Visible;
        _osdHideTimer.Stop();
        _osdHideTimer.Start();
    }

    private void UpdateOsd()
    {
        try
        {
            var t = _player.Time;
            var len = _player.Length;

            TimeText.Text = $"{FormatTime(t)} / {FormatTime(len)}";
            Progress.Value = len > 0 ? Math.Clamp((double)t / len, 0, 1) : 0;
            VolText.Text = _player.Mute ? "Muted" : $"Vol {_player.Volume}";
        }
        catch { }
    }

    private static string FormatTime(long ms)
    {
        if (ms < 0) ms = 0;
        var ts = TimeSpan.FromMilliseconds(ms);
        return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
    }

    protected override void OnClosed(EventArgs e)
    {
        _uiTimer.Stop();
        _osdHideTimer.Stop();
        _media?.Dispose();
        _player.Dispose();
        _libVlc.Dispose();
        base.OnClosed(e);
    }
}
