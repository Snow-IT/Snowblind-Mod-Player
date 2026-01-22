using System;
using System.IO;
using System.Text.Json;

namespace SnowblindModPlayer;

public static class SettingsStore
{
    public static string AppFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Snowblind-Mod Player");
    private static string FilePath => Path.Combine(AppFolder, "settings.json");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(AppFolder);
        if (!File.Exists(FilePath)) return new AppSettings();
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
