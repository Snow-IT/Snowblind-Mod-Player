using System.Linq;

namespace SnowblindModPlayer;

public static class SettingsValidator
{
    public static bool IsReadyToAutoplay(AppSettings s, MediaLibrary lib, out string reason)
    {
        reason = "";

        if (lib.Items.Count == 0)
        {
            reason = "Es sind noch keine Videos importiert.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(s.DefaultVideoId) || !lib.Items.Any(v => v.Id == s.DefaultVideoId))
        {
            reason = "Bitte ein Defaultvideo auswählen.";
            return false;
        }

        if (!HasValidMonitor(s, out var monitorReason))
        {
            reason = monitorReason;
            return false;
        }

        return true;
    }

    public static bool HasValidMonitor(AppSettings s, out string reason)
    {
        reason = "";
        var screens = System.Windows.Forms.Screen.AllScreens;

        if (screens.Length == 0)
        {
            reason = "Keine Monitore erkannt.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(s.MonitorDeviceName))
        {
            reason = "Kein Monitor ausgewählt.";
            return false;
        }

        if (!screens.Any(sc => sc.DeviceName.Equals(s.MonitorDeviceName)))
        {
            reason = $"Monitor nicht verfügbar: {s.MonitorDeviceName}";
            return false;
        }

        return true;
    }
}
