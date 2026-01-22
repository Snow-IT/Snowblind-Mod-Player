using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SnowblindModPlayer;

public partial class PlayerWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private Media? _media;
    private bool _startedFullscreen;

    public PlayerWindow()
    {
        InitializeComponent();
        Core.Initialize();
        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);
        VideoView.MediaPlayer = _player;
        Hide();
    }

    public void Play(AppSettings settings, string path)
    {
        _startedFullscreen = settings.Fullscreen;
        PlaceOnMonitor(settings);
        ApplyPlayerSettings(settings);

        _media?.Dispose();
        _media = new Media(_libVlc, new Uri(path));

        if (settings.Loop)
        {
            _player.EndReached -= OnEndReached;
            _player.EndReached += OnEndReached;
        }
        else
        {
            _player.EndReached -= OnEndReached;
        }

        _player.Play(_media);
        Show();
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    public void Stop()
    {
        _player.Stop();
        Hide();
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_media != null)
                _player.Play(_media);
        });
    }

    private void PlaceOnMonitor(AppSettings settings)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var target = screens.FirstOrDefault(s => s.DeviceName.Equals(settings.MonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                     ?? screens.First();

        var b = target.Bounds;
        Left = b.Left;
        Top = b.Top;
        Width = b.Width;
        Height = b.Height;

        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;

        if (settings.Fullscreen)
            WindowState = WindowState.Maximized;
    }

    private void ApplyPlayerSettings(AppSettings s)
    {
        _player.Mute = s.Mute;
        if (!s.Mute)
            _player.Volume = Math.Clamp(s.Volume, 0, 100);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TogglePause();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            TogglePause();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            // First ESC: exit fullscreen -> windowed. Second ESC: stop.
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                Topmost = false;
                ShowInTaskbar = true;
            }
            else
            {
                Stop();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }
    }

    private void TogglePause()
    {
        _player.Pause();
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            ShowInTaskbar = true;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _media?.Dispose();
        _player.Dispose();
        _libVlc.Dispose();
        base.OnClosed(e);
    }
}
