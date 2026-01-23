using System;
using System.IO;

namespace SnowblindModPlayer
{
    public static class LogService
    {
        // Standardpfad für Logs (AppData)
        public static string LogFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Snowblind-Mod Player",
                         "Logs");
    }
}