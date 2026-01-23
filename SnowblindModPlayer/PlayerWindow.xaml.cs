using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace SnowblindModPlayer;

public partial class PlayerWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private Media? _media;

    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _osdHideTimer;

    private bool _isFullscreen;
    private bool _shouldLoop;

    public PlayerWindow()
    {
        InitializeComponent();
        Core.Initialize();

        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);

        // EndReached abonnieren (Loop-Implementierung)
        _player.EndReached += OnPlayerEndReached;

        VideoView.MediaPlayer = _player;

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _uiTimer.Tick += (_, __) => UpdateOsd();

        _osdHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _osdHideTimer.Tick += (_, __) => { Osd.Visibility = Visibility.Collapsed; _osdHideTimer.Stop(); };

        Hide();
    }

    public void Play(AppSettings settings, string path)
    {
        // Fenster positionieren / konfigurieren zuerst
        PlaceFullscreen(settings.Fullscreen, settings.MonitorDeviceName);
        ApplyPlayerSettings(settings);

        // Loop-Flag setzen
        _shouldLoop = settings.Loop;

        // Zeige Fenster bevor LibVLC an das Render-Handle bindet.
        Show();
        Activate();
        Focus();
        Keyboard.Focus(this);

        // Starten des Media-Playbacks verzögert ausführen, damit WPF das native Handle anlegt.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                _media?.Dispose();
                _media = new Media(_libVlc, new Uri(path));
                _player.Play(_media);
            }
            catch
            {
                // Nicht kritisch für Stabilität hier — Fehler werden im UI-Log sichtbar.
            }
        }), DispatcherPriority.ApplicationIdle);

        _uiTimer.Start();

        ShowOsd($"Playing: {System.IO.Path.GetFileName(path)}");
    }

    public void Stop()
    {
        try { _player.Stop(); } catch { }
        _uiTimer.Stop();
        Hide();
    }

    private void ApplyPlayerSettings(AppSettings s)
    {
        _player.Mute = s.Mute;
        if (!s.Mute)
            _player.Volume = Math.Clamp(s.Volume, 0, 100);
    }

    // Handler für Ende des Mediums
    private void OnPlayerEndReached(object? sender, EventArgs e)
    {
        if (!_shouldLoop) return;

        // Event läuft auf einem libvlc-Thread — marshallen zum UI-Thread
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                if (_media == null) return;

                // Stop + Play neu starten ist oft stabiler als Seek(0) bei Renderer-Wechseln
                _player.Stop();
                _player.Play(_media);
            }
            catch { }
        }), DispatcherPriority.Background);
    }

    // Sicheres Umschalten / Positionieren inklusive Ab-/Ankoppeln des Video-Renderers
    private void PlaceFullscreen(bool enable, string? monitorDeviceName = null)
    {
        _isFullscreen = enable;

        // UI-Thread sicherstellen
        Dispatcher.Invoke(() =>
        {
            var wasPlaying = false;
            try { wasPlaying = _player.IsPlaying; } catch { wasPlaying = false; }

            // Wenn aktuell spielt, Pause statt Stop — vermeidet Zustandverlust
            try { if (wasPlaying) _player.Pause(); } catch { }

            // Renderer abkoppeln, damit libvlc nicht während Resize/Move auf ungültiges HWND zugreift
            try { VideoView.MediaPlayer = null; } catch { }

            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowState = WindowState.Normal;

            if (enable)
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var target = screens.FirstOrDefault(s => s.DeviceName == monitorDeviceName) ?? System.Windows.Forms.Screen.PrimaryScreen;
                var rect = target.Bounds; // Pixel

                // DPI-Konvertierung Device -> WPF DIPs
                var source = PresentationSource.FromVisual(this);
                var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                var topLeft = transform.Transform(new System.Windows.Point(rect.Left, rect.Top));
                var bottomRight = transform.Transform(new System.Windows.Point(rect.Right, rect.Bottom));

                Left = topLeft.X;
                Top = topLeft.Y;
                Width = Math.Max(1, bottomRight.X - topLeft.X);
                Height = Math.Max(1, bottomRight.Y - topLeft.Y);

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                ShowInTaskbar = false;

                // sicherstellen, dass Window die gesetzte Größe/Position benutzt
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                Topmost = false;
                ShowInTaskbar = true;
            }

            // Wieder anbinden und, falls nötig, weiter abspielen
            try
            {
                VideoView.MediaPlayer = _player;

                if (wasPlaying && _media != null)
                {
                    // Neu starten statt Resume kann stabiler sein bei Renderer-Wechsel
                    _player.Play(_media);
                }
            }
            catch { }
        });
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
                // Toggle fullscreen auf aktuellem Monitor (falls gesetzt)
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
        // EndReached abmelden
        try { _player.EndReached -= OnPlayerEndReached; } catch { }

        _uiTimer.Stop();
        _osdHideTimer.Stop();
        _media?.Dispose();
        _player.Dispose();
        _libVlc.Dispose();
        base.OnClosed(e);
    }
}
