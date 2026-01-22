using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SnowblindModPlayer;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly PlayerWindow _player;

    public event Action? ShowMainWindowRequested;
    public event Action? ExitRequested;
    public event Action? PlayDefaultRequested;
    public event Action<string>? PlayVideoRequested;
    public event Action? StopRequested;

    private MediaLibrary _library = new();

    public TrayService(PlayerWindow player)
    {
        _player = player;

        _icon = new NotifyIcon
        {
            Visible = true,
            Text = "Snowblind-Mod Player",
            Icon = SystemIcons.Application,
            ContextMenuStrip = new ContextMenuStrip()
        };

        _icon.DoubleClick += (_, __) => ShowMainWindowRequested?.Invoke();
    }

    public void Update(AppSettings settings, MediaLibrary library)
    {
        _library = library;
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var menu = _icon.ContextMenuStrip!;
        menu.Items.Clear();

        menu.Items.Add("Öffnen", null, (_, __) => ShowMainWindowRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Play Default", null, (_, __) => PlayDefaultRequested?.Invoke());
        menu.Items.Add("Stop", null, (_, __) => StopRequested?.Invoke());

        var videos = new ToolStripMenuItem("Videos (Quick Play)");
        if (_library.Items.Count == 0)
        {
            videos.DropDownItems.Add(new ToolStripMenuItem("(keine Videos)") { Enabled = false });
        }
        else
        {
            foreach (var v in _library.Items.OrderBy(x => x.DisplayName))
            {
                var id = v.Id;
                var item = new ToolStripMenuItem(v.DisplayName);
                item.Click += (_, __) => PlayVideoRequested?.Invoke(id);
                videos.DropDownItems.Add(item);
            }
        }
        menu.Items.Add(videos);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, __) => ExitRequested?.Invoke());
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
