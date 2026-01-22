using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SnowblindModPlayer.Pages;

public partial class VideosPage
{
    private MediaLibrary _library = new();

    public VideosPage()
    {
        InitializeComponent();

        Loaded += (_, __) => Reload();

        AddBtn.Click += (_, __) => AddVideos();
        RemoveBtn.Click += (_, __) => RemoveSelected();
        SetDefaultBtn.Click += (_, __) => SetDefaultFromSelection();
        ViewModeCombo.SelectionChanged += (_, __) => UpdateViewMode();
    }

    private void Reload()
    {
        _library = App.Instance.CurrentLibrary;
        DataContext = new VideosPageVm(_library, App.Instance.CurrentSettings.DefaultVideoId);
        UpdateViewMode();
    }

    private void UpdateViewMode()
    {
        var mode = ((ComboBoxItem)ViewModeCombo.SelectedItem).Content?.ToString() ?? "Liste";
        if (mode == "Miniaturen")
        {
            ListView.Visibility = Visibility.Collapsed;
            ThumbList.Visibility = Visibility.Visible;
        }
        else
        {
            ThumbList.Visibility = Visibility.Collapsed;
            ListView.Visibility = Visibility.Visible;
        }
    }

    private VideoItem? GetSelected()
    {
        if (ThumbList.Visibility == Visibility.Visible)
            return ThumbList.SelectedItem as VideoItem;
        return ListView.SelectedItem as VideoItem;
    }

    private void AddVideos()
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Import fehlgeschlagen: {Path.GetFileName(file)} {ex.Message}",
                    "Snowblind-Mod Player", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        App.Instance.SaveLibrary(_library);
        Reload();
    }

    private void RemoveSelected()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            System.Windows.MessageBox.Show("Bitte ein Video auswählen.", "Snowblind-Mod Player");
            return;
        }

        if (System.Windows.MessageBox.Show($"Video entfernen? {selected.DisplayName}", "Snowblind-Mod Player",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try { MediaImportService.RemoveFromAppData(selected); } catch { }

        _library.Items.Remove(selected);

        // If removed default -> clear default
        var s = App.Instance.CurrentSettings;
        if (s.DefaultVideoId == selected.Id)
        {
            s.DefaultVideoId = _library.Items.FirstOrDefault()?.Id ?? "";
            App.Instance.SaveSettings(s);
        }

        App.Instance.SaveLibrary(_library);
        Reload();
    }

    private void SetDefaultFromSelection()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            System.Windows.MessageBox.Show("Bitte ein Video auswählen.", "Snowblind-Mod Player");
            return;
        }

        var s = App.Instance.CurrentSettings;
        s.DefaultVideoId = selected.Id;
        App.Instance.SaveSettings(s);

        Reload();
        System.Windows.MessageBox.Show($"Defaultvideo gesetzt: {selected.DisplayName}",
            "Snowblind-Mod Player", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

internal class VideosPageVm
{
    public System.Collections.ObjectModel.ObservableCollection<VideoItem> Items { get; }

    public VideosPageVm(MediaLibrary lib, string defaultId)
    {
        Items = lib.Items;
        foreach (var it in Items)
        {
            it.IsDefault = it.Id == defaultId;
            it.DefaultStarOpacity = it.IsDefault ? 1.0 : 0.15;
        }
    }
}
