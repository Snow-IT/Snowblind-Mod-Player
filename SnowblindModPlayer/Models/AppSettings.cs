namespace SnowblindModPlayer;

public class AppSettings
{
    public string? MonitorDeviceName { get; set; }

    public string DefaultVideoId { get; set; } = "";

    public bool Fullscreen { get; set; } = true;
    public bool Loop { get; set; } = true;
    public bool Mute { get; set; } = false;
    public int Volume { get; set; } = 100;

    public bool AutoPlayOnStart { get; set; } = true;
    public int StartDelaySeconds { get; set; } = 0;
}
