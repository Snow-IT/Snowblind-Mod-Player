using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnowblindModPlayer;

public class VideoItem : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string? ThumbnailPath { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    private bool _isDefault;
    public bool IsDefault
    {
        get => _isDefault;
        set { if (_isDefault != value) { _isDefault = value; OnPropertyChanged(); } }
    }

    private double _defaultStarOpacity = 0.12;
    public double DefaultStarOpacity
    {
        get => _defaultStarOpacity;
        set { if (Math.Abs(_defaultStarOpacity - value) > 0.0001) { _defaultStarOpacity = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
