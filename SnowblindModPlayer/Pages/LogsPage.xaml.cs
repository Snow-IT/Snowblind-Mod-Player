using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SnowblindModPlayer; // damit LogService ohne weiteren using gefunden wird

namespace SnowblindModPlayer.Pages;

public partial class LogsPage : Page
{
    public LogsPage()
    {
        InitializeComponent();
        Loaded += (_, __) => _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var folder = LogService.LogFolder;
            if (!Directory.Exists(folder))
            {
                LogList.ItemsSource = Array.Empty<FileInfo>();
                LogContent.Text = "Keine Logs vorhanden.";
                return;
            }

            var files = new DirectoryInfo(folder)
                        .GetFiles("log-*.log")
                        .OrderByDescending(f => f.CreationTimeUtc)
                        .ToArray();

            LogList.ItemsSource = files;
            if (files.Length > 0)
            {
                LogList.SelectedIndex = 0;
            }
            else
            {
                LogContent.Text = "Keine Logs vorhanden.";
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync("Fehler", ex.Message);
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var fi = LogList.SelectedItem as FileInfo;
        if (fi == null)
        {
            LogContent.Text = string.Empty;
            return;
        }

        try
        {
            using var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            LogContent.Text = text;
            LogContent.ScrollToHome();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync("Fehler beim Laden", ex.Message);
        }
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var fi = LogList.SelectedItem as FileInfo;
        if (fi == null)
        {
            await DialogService.ShowMessageAsync("Keine Auswahl", "Bitte eine Logdatei auswählen.");
            return;
        }

        var ok = await DialogService.ConfirmAsync("Log löschen", $"Möchtest du die Datei {fi.Name} löschen?", primary: "Löschen");
        if (!ok) return;

        try
        {
            File.Delete(fi.FullName);
            await DialogService.ToastAsync($"Gelöscht: {fi.Name}");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync("Löschen fehlgeschlagen", ex.Message);
        }
    }

    private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = LogService.LogFolder;
            Directory.CreateDirectory(folder);
            var psi = new ProcessStartInfo(folder) { UseShellExecute = true };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowMessageAsync("Ordner öffnen fehlgeschlagen", ex.Message);
        }
    }
}