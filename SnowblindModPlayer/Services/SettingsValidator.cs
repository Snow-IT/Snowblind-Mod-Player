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

        return true;
    }
}
