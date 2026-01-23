using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnowblindModPlayer.Pages;

public partial class VideosPage
{
    private MediaLibrary _library = new();

    public VideosPage()
    {
        InitializeComponent();

        ViewModeCombo.SelectedIndex = 1; // Default: Miniaturen

        Loaded += (_, __) => Reload();

        AddBtn.Click += async (_, __) => await AddVideosAsync();
        RemoveBtn.Click += async (_, __) => await RemoveSelectedAsync();
        SetDefaultBtn.Click += async (_, __) => await SetDefaultFromSelectionAsync();
        ViewModeCombo.SelectionChanged += (_, __) => UpdateViewMode();
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
        var mode = ((ComboBoxItem)ViewModeCombo.SelectedItem).Content?.ToString() ?? "Miniaturen";
        if (mode == "Liste")
        {
            ListView.Visibility = Visibility.Visible;
            ThumbScroll.Visibility = Visibility.Collapsed;
        }
        else
        {
            ListView.Visibility = Visibility.Collapsed;
            ThumbScroll.Visibility = Visibility.Visible;
        }
    }

    private VideoItem? GetSelected()
        => ListView.Visibility == Visibility.Visible ? ListView.SelectedItem as VideoItem : ThumbList.SelectedItem as VideoItem;

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
                var item = MediaImportService.ImportToAppData(file);
                _library.Items.Add(item);

                // set first imported as default if none
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

        ListView.SelectedItem = selected;
        ThumbList.SelectedItem = selected;

        await DialogService.ToastAsync($"Default gesetzt: {selected.DisplayName}");
    }

    // C: ensure trackpad scrolling works even when mouse is over inner elements
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ThumbScroll.Visibility == Visibility.Visible)
        {
            ThumbScroll.ScrollToVerticalOffset(ThumbScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
