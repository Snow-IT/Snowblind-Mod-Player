using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;

namespace SnowblindModPlayer.Pages;

public partial class VideosPage
{
    private MediaLibrary _library = new();

    public VideosPage()
    {
        InitializeComponent();

        ViewModeCombo!.SelectedIndex = 1; // Default: Miniaturen

        Loaded += (_, __) => Reload();

        AddBtn!.Click += async (_, __) => await AddVideosAsync();
        RemoveBtn!.Click += async (_, __) => await RemoveSelectedAsync();
        SetDefaultBtn!.Click += async (_, __) => await SetDefaultFromSelectionAsync();
        ViewModeCombo!.SelectionChanged += (_, __) => UpdateViewMode();
    }

    private void Reload()
    {
        _library = App.Instance.CurrentLibrary;
        DataContext = _library;
        App.Instance.ApplyDefaultMarkers();
        UpdateViewMode();
    }

    private void UpdateViewMode()
    {
        var mode = ((ComboBoxItem)ViewModeCombo!.SelectedItem).Content?.ToString() ?? "Miniaturen";
        if (mode == "Liste")
        {
            ListView!.Visibility = Visibility.Visible;
            ThumbList!.Visibility = Visibility.Collapsed;
        }
        else
        {
            ListView!.Visibility = Visibility.Collapsed;
            ThumbList!.Visibility = Visibility.Visible;
        }
    }

    private VideoItem? GetSelected()
        => ListView!.Visibility == Visibility.Visible ? ListView!.SelectedItem as VideoItem : ThumbList!.SelectedItem as VideoItem;

    private async Task AddVideosAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "Video files|*.mp4;*.webm;*.mov;*.wmv;*.avi;*.mkv|All files|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        foreach (var file in dlg.FileNames)
        {
            try
            {
                var item = await MediaImportService.ImportToAppDataAsync(file); // async implemented
                _library.Items.Add(item);

                if (string.IsNullOrWhiteSpace(App.Instance.CurrentSettings.DefaultVideoId))
                {
                    var s = App.Instance.CurrentSettings;
                    s.DefaultVideoId = item.Id;
                    App.Instance.SaveSettings(s);
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowMessageAsync("Import fehlgeschlagen", $"{Path.GetFileName(file)}\n{ex.Message}");
            }
        }

        App.Instance.SaveLibrary(_library);
        Reload();
    }

    private async Task RemoveSelectedAsync()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            await DialogService.ShowMessageAsync("Kein Video ausgewählt", "Bitte ein Video auswählen.");
            return;
        }

        var ok = await DialogService.ConfirmAsync("Video entfernen", $"Möchtest du dieses Video entfernen?\n\n{selected.DisplayName}", primary: "Entfernen");
        if (!ok) return;

        try { MediaImportService.RemoveFromAppData(selected); } catch { }

        _library.Items.Remove(selected);

        var s = App.Instance.CurrentSettings;
        if (s.DefaultVideoId == selected.Id)
            s.DefaultVideoId = _library.Items.FirstOrDefault()?.Id ?? "";

        App.Instance.SaveSettings(s);
        App.Instance.SaveLibrary(_library);
        Reload();
    }

    private async Task SetDefaultFromSelectionAsync()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            await DialogService.ShowMessageAsync("Kein Video ausgewählt", "Bitte ein Video auswählen.");
            return;
        }

        var s = App.Instance.CurrentSettings;
        s.DefaultVideoId = selected.Id;
        App.Instance.SaveSettings(s);
        App.Instance.ApplyDefaultMarkers();

        ListView!.SelectedItem = selected;
        ThumbList!.SelectedItem = selected;

        await DialogService.ToastAsync($"Default gesetzt: {selected.DisplayName}");
    }

    // Trackpad / Mausrad scrolling: finde ScrollViewer innerhalb von ThumbList und scrolle
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ThumbList == null || ThumbList.Visibility != Visibility.Visible) return;

        var sv = FindDescendantScrollViewer(ThumbList);
        if (sv != null)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
    {
        if (root == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer sv) return sv;
            var found = FindDescendantScrollViewer(child);
            if (found != null) return found;
        }
        return null;
    }
}
