using System;

namespace SnowblindModPlayer;

public class VideoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string? ThumbnailPath { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    // UI helpers
    public bool IsDefault { get; set; }
    public double DefaultStarOpacity { get; set; } = 0.15;
}
