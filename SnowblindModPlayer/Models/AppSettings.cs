namespace SnowblindModPlayer;

public class AppSettings
{
    public string DefaultVideoId { get; set; } = "";

    // Neu: entspricht SettingsPage.xaml.cs
    public bool AutoPlayOnStart { get; set; } = false;
    public string MonitorDeviceName { get; set; } = "";

    public bool Fullscreen { get; set; } = true;
    public bool Loop { get; set; } = true;
    public bool Mute { get; set; } = false;
    public int Volume { get; set; } = 100;

    public int StartDelaySeconds { get; set; } = 0;

    // Neu: Für Audio-Only Modus
    public bool AudioOnly { get; set; } = false;

    // Neu: erweitertes Logging aktivieren
    public bool AdvancedLogging { get; set; } = false;
}
