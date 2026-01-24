using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;
using System.Windows.Data;
using System.ComponentModel;

namespace SnowblindModPlayer.Pages
{
    public partial class VideosPage : Page
    {
        private MediaLibrary _library = new();

        public VideosPage()
        {
            InitializeComponent();

            Loaded += (_, __) => Reload();

            // Keine lokalen Toolbar-Buttons mehr: Host-Buttons rufen die folgenden Host-Methoden auf.
            // ViewMode wird per Host-Combo gesteuert -> SetViewModeFromHost implementiert.
        }

        private void Reload()
        {
            _library = App.Instance.CurrentLibrary;
            DataContext = _library;

            // Filterung: entferne/null-fälle aus der Ansicht (zeigt keine leeren/invaliden Einträge)
            var view = CollectionViewSource.GetDefaultView(_library.Items);
            if (view != null)
            {
                view.Filter = o =>
                {
                    if (o is not VideoItem v) return false;
                    if (string.IsNullOrWhiteSpace(v.DisplayName)) return false;
                    // Optional: nur existierende Dateien anzeigen
                    if (string.IsNullOrWhiteSpace(v.StoredPath) || !File.Exists(v.StoredPath)) return false;
                    return true;
                };
                view.Refresh();
            }

            App.Instance.ApplyDefaultMarkers();
            UpdateViewMode(); // initial
        }

        private void UpdateViewMode()
        {
            // Fallback: wenn ThumbList sichtbar, zeigen wir Thumbs, sonst Liste
            if (ThumbList != null && ThumbList.Visibility == Visibility.Visible)
            {
                ListView!.Visibility = Visibility.Collapsed;
                ThumbList!.Visibility = Visibility.Visible;
            }
            else
            {
                ListView!.Visibility = Visibility.Visible;
                ThumbList!.Visibility = Visibility.Collapsed;
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
                    var item = await MediaImportService.ImportToAppDataAsync(file);
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

        // Allgemeiner PreviewMouseWheel-Handler: findet nächstes ScrollViewer-Element unter Maus / OriginalSource und scrollt pixel-basiert
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Versuche zuerst das Element unter dem Mauszeiger zu nutzen
            var dep = e.OriginalSource as DependencyObject ?? Mouse.DirectlyOver as DependencyObject;
            var sv = FindAncestorScrollViewer(dep);

            // Fallback: Suche im sichtbaren Bereich (ThumbList oder ListView)
            if (sv == null)
            {
                if (ThumbList != null && ThumbList.Visibility == Visibility.Visible)
                    sv = FindDescendantScrollViewer(ThumbList);
                else if (ListView != null && ListView.Visibility == Visibility.Visible)
                    sv = FindDescendantScrollViewer(ListView);
            }

            if (sv != null)
            {
                // Pixel-basiertes Scrollen; e.Delta ist ±120 pro Notch -> umrechnen
                var newOffset = sv.VerticalOffset - (e.Delta);
                if (newOffset < 0) newOffset = 0;
                if (newOffset > sv.ExtentHeight) newOffset = sv.ExtentHeight;
                sv.ScrollToVerticalOffset(newOffset);
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

        private ScrollViewer? FindAncestorScrollViewer(DependencyObject? child)
        {
            var current = child;
            while (current != null)
            {
                if (current is ScrollViewer sv) return sv;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // Host‑API: Aufrufe aus MainWindow (Top-Buttons) landen hier

        internal async Task AddVideosAsyncFromHost()
        {
            await AddVideosAsync();
        }

        internal async Task RemoveSelectedFromHost()
        {
            await RemoveSelectedAsync();
        }

        internal async Task SetDefaultFromHost()
        {
            await SetDefaultFromSelectionAsync();
        }

        internal void SetViewModeFromHost(string mode)
        {
            if (mode == "Liste")
            {
                if (ListView != null) ListView.Visibility = Visibility.Visible;
                if (ThumbList != null) ThumbList.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (ListView != null) ListView.Visibility = Visibility.Collapsed;
                if (ThumbList != null) ThumbList.Visibility = Visibility.Visible;
            }
        }
    }
}
