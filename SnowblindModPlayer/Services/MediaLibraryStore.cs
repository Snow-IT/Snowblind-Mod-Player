using System;
using System.IO;
using System.Text.Json;

namespace SnowblindModPlayer;

public static class MediaLibraryStore
{
    public static string AppFolder => SettingsStore.AppFolder;
    public static string MediaFolder => Path.Combine(AppFolder, "Media");
    public static string ThumbsFolder => Path.Combine(AppFolder, "Thumbs");
    private static string LibraryPath => Path.Combine(AppFolder, "library.json");

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(AppFolder);
        Directory.CreateDirectory(MediaFolder);
        Directory.CreateDirectory(ThumbsFolder);
    }

    public static MediaLibrary Load()
    {
        EnsureFolders();
        if (!File.Exists(LibraryPath)) return new MediaLibrary();
        return JsonSerializer.Deserialize<MediaLibrary>(File.ReadAllText(LibraryPath)) ?? new MediaLibrary();
    }

    public static void Save(MediaLibrary lib)
    {
        EnsureFolders();
        var json = JsonSerializer.Serialize(lib, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(LibraryPath, json);
    }
}
