using System.Collections.ObjectModel;

namespace SnowblindModPlayer;

public class MediaLibrary
{
    public ObservableCollection<VideoItem> Items { get; set; } = new();
}
