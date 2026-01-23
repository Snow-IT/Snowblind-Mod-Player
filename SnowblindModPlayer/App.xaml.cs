using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SnowblindModPlayer;

public partial class App : System.Windows.Application
{
    public static App Instance => (App)System.Windows.Application.Current;

    private TrayService? _tray;
    private PlayerWindow? _player;

    public AppSettings CurrentSettings { get; private set; } = new();
    public MediaLibrary CurrentLibrary { get; private set; } = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        MediaLibraryStore.EnsureFolders();
        CurrentSettings = SettingsStore.Load();
        CurrentLibrary = MediaLibraryStore.Load();
        ApplyDefaultMarkers();

        _player = new PlayerWindow();
        _tray = new TrayService();
        _tray.ShowMainWindowRequested += () => ShowMainWindow();
        _tray.ExitRequested += () => ExitApp();
        _tray.PlayDefaultRequested += () => TryPlayDefault();
        _tray.PlayVideoRequested += id => TryPlayById(id);
        _tray.StopRequested += () => _player.Stop();
        _tray.Update(CurrentLibrary);

        var args = e.Args.Select(a => a.ToLowerInvariant()).ToArray();
        var forceSettings = args.Contains("--settings");

        if (forceSettings)
        {
            ShowMainWindow();
            return;
        }

        // Autoplay directly (as desired)
        var ok = SettingsValidator.IsReadyToAutoplay(CurrentSettings, CurrentLibrary, out var reason);
        if (!ok)
        {
            ShowMainWindow(reason);
            return;
        }

        if (CurrentSettings.StartDelaySeconds > 0)
            await Task.Delay(CurrentSettings.StartDelaySeconds * 1000);

        TryPlayDefault();
    }

    public void SaveSettings(AppSettings newSettings)
    {
        CurrentSettings = newSettings;
        SettingsStore.Save(CurrentSettings);
        ApplyDefaultMarkers();
        _tray?.Update(CurrentLibrary);
    }

    public void SaveLibrary(MediaLibrary library)
    {
        CurrentLibrary = library;
        MediaLibraryStore.Save(CurrentLibrary);
        ApplyDefaultMarkers();
        _tray?.Update(CurrentLibrary);
    }

    public void ApplyDefaultMarkers()
    {
        var defaultId = CurrentSettings.DefaultVideoId;
        foreach (var it in CurrentLibrary.Items)
        {
            it.IsDefault = it.Id == defaultId;
            it.DefaultStarOpacity = it.IsDefault ? 1.0 : 0.12;
        }
    }

    private void TryPlayDefault()
    {
        var item = CurrentLibrary.Items.FirstOrDefault(v => v.Id == CurrentSettings.DefaultVideoId);
        if (item == null || !System.IO.File.Exists(item.StoredPath))
        {
            ShowMainWindow("Defaultvideo ist nicht verfügbar. Bitte neu auswählen.");
            return;
        }

        _player?.Play(CurrentSettings, item.StoredPath);
    }

    private void TryPlayById(string id)
    {
        var item = CurrentLibrary.Items.FirstOrDefault(v => v.Id == id);
        if (item == null || !System.IO.File.Exists(item.StoredPath))
            return;

        _player?.Play(CurrentSettings, item.StoredPath);
    }

    private void ShowMainWindow(string? hint = null)
    {
        var win = Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (win == null)
        {
            win = new MainWindow();
            win.Show();
        }
        else
        {
            win.Show();
            win.Activate();
        }

        if (!string.IsNullOrWhiteSpace(hint))
            win.SetHint(hint);

        win.RefreshBindings();
    }

    private void ExitApp()
    {
        _player?.Close();
        _tray?.Dispose();
        Shutdown();
    }
}
