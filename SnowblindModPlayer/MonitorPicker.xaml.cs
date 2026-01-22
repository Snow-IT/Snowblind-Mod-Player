using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnowblindModPlayer;

public partial class MonitorPicker : System.Windows.Controls.UserControl
{
    public string? SelectedDeviceName { get; private set; }
    public event Action? SelectionChanged;

    private System.Windows.Forms.Screen[] _screens = Array.Empty<System.Windows.Forms.Screen>();

    public MonitorPicker()
    {
        InitializeComponent();
        SizeChanged += (_, __) => Redraw();
    }

    public void LoadScreens(string? selectedDeviceName)
    {
        _screens = System.Windows.Forms.Screen.AllScreens;
        SelectedDeviceName = selectedDeviceName;

        if (!string.IsNullOrWhiteSpace(SelectedDeviceName) && !_screens.Any(s => s.DeviceName == SelectedDeviceName))
            SelectedDeviceName = null;

        if (SelectedDeviceName == null && _screens.Length > 0)
            SelectedDeviceName = _screens[0].DeviceName;

        Redraw();
    }

    private void Redraw()
    {
        Canvas.Children.Clear();
        if (_screens.Length == 0) return;

        var minX = _screens.Min(s => s.Bounds.Left);
        var minY = _screens.Min(s => s.Bounds.Top);
        var maxX = _screens.Max(s => s.Bounds.Right);
        var maxY = _screens.Max(s => s.Bounds.Bottom);

        var totalW = Math.Max(1, maxX - minX);
        var totalH = Math.Max(1, maxY - minY);

        var cw = Math.Max(10, ActualWidth - 20);
        var ch = Math.Max(10, ActualHeight - 20);

        var scale = Math.Min(cw / totalW, ch / totalH);
        var offsetX = (cw - totalW * scale) / 2.0;
        var offsetY = (ch - totalH * scale) / 2.0;

        int idx = 1;
        foreach (var s in _screens)
        {
            var b = s.Bounds;

            var x = offsetX + (b.Left - minX) * scale;
            var y = offsetY + (b.Top - minY) * scale;
            var w = Math.Max(20, b.Width * scale);
            var h = Math.Max(20, b.Height * scale);

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = w,
                Height = h,
                RadiusX = 8,
                RadiusY = 8,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x22, 0x00, 0x00, 0x00)),
                Stroke = s.DeviceName == SelectedDeviceName
                    ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x4C, 0xA6, 0xFF))
                    : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            rect.MouseLeftButtonDown += (_, __) =>
            {
                SelectedDeviceName = s.DeviceName;
                SelectionChanged?.Invoke();
                Redraw();
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            Canvas.Children.Add(rect);

            var label = new TextBlock
            {
                Text = idx.ToString() + (s.Primary ? " (Main)" : ""),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold
            };

            Canvas.SetLeft(label, x + 10);
            Canvas.SetTop(label, y + 10);
            Canvas.Children.Add(label);

            idx++;
        }
    }
}
