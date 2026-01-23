using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SnowblindModPlayer;

public static class MediaImportService
{
    private static readonly string[] AllowedExt = new[] { ".mp4", ".webm", ".mov", ".wmv", ".avi", ".mkv" };

    public static VideoItem ImportToAppData(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source not found", sourcePath);

        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (Array.IndexOf(AllowedExt, ext) < 0)
            throw new InvalidOperationException("Unsupported format: " + ext);

        MediaLibraryStore.EnsureFolders();

        var id = Guid.NewGuid().ToString("N");
        var safeName = Path.GetFileName(sourcePath);
        var folder = Path.Combine(MediaLibraryStore.MediaFolder, id);
        Directory.CreateDirectory(folder);

        var destPath = Path.Combine(folder, safeName);
        File.Copy(sourcePath, destPath, true);

        var thumbPath = Path.Combine(MediaLibraryStore.ThumbsFolder, id + ".jpg");

        var item = new VideoItem
        {
            Id = id,
            DisplayName = Path.GetFileNameWithoutExtension(safeName),
            FileName = safeName,
            StoredPath = destPath,
            ThumbnailPath = null,
            ImportedAt = DateTime.Now
        };

        TryGenerateThumbnail(destPath, thumbPath);
        if (File.Exists(thumbPath))
            item.ThumbnailPath = thumbPath;

        return item;
    }

    public static void RemoveFromAppData(VideoItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.StoredPath))
        {
            var dir = Directory.GetParent(item.StoredPath)?.FullName;
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        if (!string.IsNullOrWhiteSpace(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
            File.Delete(item.ThumbnailPath);
    }

    // Async wrapper führt die synchrone Arbeit im Background-Thread aus
    internal static Task<VideoItem> ImportToAppDataAsync(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            throw new ArgumentException("file is required", nameof(file));

        return Task.Run(() => ImportToAppData(file));
    }

    private static void TryGenerateThumbnail(string videoPath, string thumbPath)
    {
        try
        {
            Core.Initialize();
            using var lib = new LibVLC();
            using var mp = new MediaPlayer(lib);
            using var media = new Media(lib, new Uri(videoPath));

            mp.Play(media);
            Task.Delay(800).Wait();
            mp.TakeSnapshot(0, thumbPath, 320, 180);
            mp.Stop();
        }
        catch
        {
            // ignore thumbnail failures
        }
    }
}
