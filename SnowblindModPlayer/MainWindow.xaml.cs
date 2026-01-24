using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // neu: für Fade-Animation
using System.Windows.Threading;
using Wpf.Ui.Controls; // neu: für DispatcherPriority

namespace SnowblindModPlayer;

public partial class MainWindow
{
    // cache für aktuell sichtbare Page -> verhindert unnötiges Rebuild
    private Type? _currentPageType;

    public MainWindow()
    {
        InitializeComponent();

        // DialogService initialisieren nur wenn Host vorhanden
        if (RootDialogHost != null)
            DialogService.Initialize(RootDialogHost);

        // Global mouse wheel routing: erlaubt Scrollen ohne Fokus auf Scrollbar
        this.PreviewMouseWheel += MainWindow_PreviewMouseWheel;

        // ViewModeComboTop: vollqualifizierter Typ vermeidet Mehrdeutigkeit
        var vmCombo = FindElement<System.Windows.Controls.ComboBox>("ViewModeComboTop");
        if (vmCombo != null)
        {
            vmCombo.SelectionChanged += (_, __) =>
            {
                var mode = ((System.Windows.Controls.ComboBoxItem?)vmCombo.SelectedItem)?.Content?.ToString() ?? "Miniaturen";
                _ = InvokeOnActiveVideosPageAsync(p => { p.SetViewModeFromHost(mode); return Task.CompletedTask; });
            };
        }

        // Falls PageButtons in XAML existieren (ältere Version), keine Ausnahme werfen: wir binden nur wenn vorhanden
        var addBtn = FindElement<System.Windows.Controls.Button>("AddBtnTop");
        if (addBtn != null) addBtn.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.AddVideosAsyncFromHost());

        var remBtn = FindElement<System.Windows.Controls.Button>("RemoveBtnTop");
        if (remBtn != null) remBtn.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.RemoveSelectedFromHost());

        var defBtn = FindElement<System.Windows.Controls.Button>("SetDefaultBtnTop");
        if (defBtn != null) defBtn.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.SetDefaultFromHost());
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (Nav == null) return;

        // initial navigation (nur auf Windows)
        if (OperatingSystem.IsWindows())
            Nav.Navigate(typeof(Pages.VideosPage));

        // Auswahl-Event beobachten — NavigationView feuert SelectionChanged / ItemInvoked
        try { Nav.SelectionChanged += Nav_SelectionChanged; } catch { /* ignore */ }
        try { Nav.ItemInvoked += Nav_ItemInvoked; } catch { /* ignore */ }

        // Initial prüfen (leicht verzögert, damit Navigation ausgeführt ist)
        Dispatcher.InvokeAsync(DetermineAndApplyPageButtons, DispatcherPriority.Background);
    }

    private void Nav_SelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        // XAML-Event-Handler rief bisher NotImplementedException -> Absturz beim Klick auf NavigationItems.
        // Stattdessen aktualisieren wir die TitleBar-Buttons (robustere, bereits vorhandene Methode).
        try
        {
            UpdateTopButtonsVisibility();
        }
        catch
        {
            // Falls UpdateTopButtonsVisibility aus irgendeinem Grund fehlt/fehlschlägt,
            // zur Sicherheit einen leichteren Fallback aufrufen (nur keine Ausnahme werfen).
            try { Dispatcher.InvokeAsync(UpdateTopButtonsVisibility, System.Windows.Threading.DispatcherPriority.Background); } catch { }
        }
    }

    private void Nav_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // direkt prüfen — sehr günstig
        DetermineAndApplyPageButtons();
    }

    private void Nav_ItemInvoked(object? sender, EventArgs e)
    {
        // Versuche den Zieltyp direkt aus den EventArgs zu extrahieren (kommt früher als SelectedItem)
        Type? target = null;

        try
        {
            if (e != null)
            {
                // gängige Property-Namen probieren: "InvokedItemContainer" oder "InvokedItem"
                var prop = e.GetType().GetProperty("InvokedItemContainer") ?? e.GetType().GetProperty("InvokedItem");
                if (prop != null)
                {
                    var invoked = prop.GetValue(e);
                    if (invoked != null)
                    {
                        // NavigationViewItem hat normalerweise TargetPageType
                        var targetProp = invoked.GetType().GetProperty("TargetPageType");
                        if (targetProp != null)
                            target = targetProp.GetValue(invoked) as Type;

                        // Fallback: manchmal ist invoked selbst ein NavigationViewItem oder ein DataContext mit TargetPageType
                        if (target == null)
                        {
                            // DataContext prüfen
                            var dcProp = invoked.GetType().GetProperty("DataContext");
                            if (dcProp != null)
                            {
                                var dc = dcProp.GetValue(invoked);
                                var t2 = dc?.GetType().GetProperty("TargetPageType")?.GetValue(dc) as Type;
                                if (t2 != null) target = t2;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // kein Fehler werfen — Fallback unten
        }

        if (target != null)
        {
            // Sofort die TitleBar umbauen, noch bevor Navigation fertig ist
            _currentPageType = target;
            RebuildTitleBarButtonsFor(target);
        }
        else
        {
            // Fallback: wie bisher (etwas später)
            DetermineAndApplyPageButtons();
        }
    }

    private void DetermineAndApplyPageButtons()
    {
        // 1) Versuche TargetPageType aus SelectedItem (sauber & performant)
        Type? target = null;
        try
        {
            var sel = Nav?.SelectedItem;
            if (sel != null)
            {
                // NavigationViewItem hat TargetPageType in deiner XAML (Wpf.Ui lib)
                if (sel is Wpf.Ui.Controls.NavigationViewItem nvi)
                    target = nvi.TargetPageType;
                else
                {
                    // sometimes SelectedItem is a container, try dynamic probe
                    var prop = sel.GetType().GetProperty("TargetPageType");
                    if (prop != null) target = prop.GetValue(sel) as Type;
                }
            }
        }
        catch { /* ignore */ }

        // 2) Fallback: falls target null, prüfe ob im Nav bereits eine Seite existiert (einmaliger, kurzer Scan)
        if (target == null)
        {
            var page = FindDescendant<Pages.VideosPage>(Nav);
            if (page != null) target = typeof(Pages.VideosPage);
            else if (FindDescendant<Pages.LogsPage>(Nav) != null) target = typeof(Pages.LogsPage);
        }

        // Wenn sich nichts geändert hat: nichts tun
        if (target == _currentPageType) return;
        _currentPageType = target;

        // Buttons umbauen je nach Zieltyp
        RebuildTitleBarButtonsFor(target);
    }

    private void RebuildTitleBarButtonsFor(Type? pageType)
    {
        var pageHost = FindElement<System.Windows.Controls.StackPanel>("PageButtonsHost");
        if (pageHost == null)
        {
            // Fallback: steuere legacy-named Buttons ein einziges Mal
            bool showVideos = pageType == typeof(Pages.VideosPage);
            SetVisibilityByName("AddBtnTop", showVideos);
            SetVisibilityByName("RemoveBtnTop", showVideos);
            SetVisibilityByName("SetDefaultBtnTop", showVideos);

            var vmCombo = FindElement<System.Windows.Controls.ComboBox>("ViewModeComboTop");
            if (vmCombo != null) vmCombo.Visibility = showVideos ? Visibility.Visible : Visibility.Collapsed;

            var viewLabel = FindElement<System.Windows.Controls.TextBlock>("ViewLabelTop");
            if (viewLabel != null) viewLabel.Visibility = showVideos ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        // clear and create only when changed (we already ensured pageType changed)
        pageHost.Children.Clear();

        if (pageType == typeof(Pages.VideosPage))
        {
            var add = new System.Windows.Controls.Button { Content = "+ Hinzufügen", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            add.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.AddVideosAsyncFromHost());
            pageHost.Children.Add(add);

            var remove = new System.Windows.Controls.Button { Content = "Entfernen", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            remove.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.RemoveSelectedFromHost());
            pageHost.Children.Add(remove);

            var setDefault = new System.Windows.Controls.Button { Content = "Als Default", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            setDefault.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.SetDefaultFromHost());
            pageHost.Children.Add(setDefault);

            // ViewMode sichtbar (falls vorhanden)
            var vmCombo = FindElement<System.Windows.Controls.ComboBox>("ViewModeComboTop");
            if (vmCombo != null) vmCombo.Visibility = Visibility.Visible;
            var viewLabel = FindElement<System.Windows.Controls.TextBlock>("ViewLabelTop");
            if (viewLabel != null) viewLabel.Visibility = Visibility.Visible;

            pageHost.Visibility = Visibility.Visible;
            return;
        }

        if (pageType == typeof(Pages.LogsPage))
        {
            var refresh = new System.Windows.Controls.Button { Content = "Refresh", Margin = new Thickness(0, 0, 8, 0) };
            refresh.Click += async (_, __) => await InvokeOnActiveLogsPageAsync(p => p.RefreshAsync());
            pageHost.Children.Add(refresh);

            var del = new System.Windows.Controls.Button { Content = "Löschen", Margin = new Thickness(0, 0, 8, 0) };
            del.Click += async (_, __) => await InvokeOnActiveLogsPageAsync(p => p.DeleteSelectedAsync());
            pageHost.Children.Add(del);

            var open = new System.Windows.Controls.Button { Content = "Ordner öffnen", Margin = new Thickness(0, 0, 8, 0) };
            open.Click += (_, __) => _ = InvokeOnActiveLogsPageAsync(p => { p.OpenFolder(); return Task.CompletedTask; });
            pageHost.Children.Add(open);

            // ViewMode ausblenden
            var vmCombo = FindElement<System.Windows.Controls.ComboBox>("ViewModeComboTop");
            if (vmCombo != null) vmCombo.Visibility = Visibility.Collapsed;
            var viewLabel = FindElement<System.Windows.Controls.TextBlock>("ViewLabelTop");
            if (viewLabel != null) viewLabel.Visibility = Visibility.Collapsed;

            pageHost.Visibility = Visibility.Visible;
            return;
        }

        // keine Seite aktiv
        pageHost.Visibility = Visibility.Collapsed;
    }

    // Erweitert: optional autoHide in ms (0 = kein AutoHide)
    public async void SetHint(string message, int autoHideMs = 0)
    {
        try
        {
            var hintBar = FindElement<dynamic>("HintBar");
            if (hintBar == null) return;

            hintBar.Message = message;
            hintBar.Opacity = 1.0;
            hintBar.IsOpen = true;

            if (autoHideMs > 0)
            {
                await Task.Delay(autoHideMs);

                var fade = new DoubleAnimation(1.0, 0.0, new System.TimeSpan(0, 0, 0, 0, 400));
                fade.FillBehavior = FillBehavior.Stop;
                fade.Completed += (_, __) =>
                {
                    try { hintBar.IsOpen = false; hintBar.Opacity = 1.0; } catch { }
                };
                (hintBar as UIElement)?.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }
        catch
        {
            // Fehler beim Anzeigen des Hints dürfen die App nicht stören
        }
    }

    // Ersetze die vorhandene UpdateTopButtonsVisibility-Methode durch diese Version
    private void UpdateTopButtonsVisibility()
    {
        bool isVideos = false;
        bool isLogs = false;

        try
        {
            if (Nav != null)
            {
                isVideos = FindDescendant<Pages.VideosPage>(Nav) != null;
                isLogs = FindDescendant<Pages.LogsPage>(Nav) != null;
            }

            if (!isVideos)
            {
                isVideos = FindDescendantByName<System.Windows.Controls.ListBox>(this, "ThumbList") != null
                           || FindDescendantByName<System.Windows.Controls.ListView>(this, "ListView") != null;
            }

            if (!isLogs)
            {
                isLogs = FindDescendantByName<System.Windows.Controls.ListBox>(this, "LogList") != null
                         || FindDescendantByName<System.Windows.Controls.TextBox>(this, "LogContent") != null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("UpdateTopButtonsVisibility: detection error: " + ex.Message);
        }

        System.Diagnostics.Debug.WriteLine($"UpdateTopButtonsVisibility: isVideos={isVideos}, isLogs={isLogs}");

        var pageHost = FindElement<System.Windows.Controls.StackPanel>("PageButtonsHost");
        if (pageHost == null)
        {
            System.Diagnostics.Debug.WriteLine("UpdateTopButtonsVisibility: PageButtonsHost not found in visual tree.");
            // fallback: try legacy buttons
            SetVisibilityByName("AddBtnTop", isVideos);
            SetVisibilityByName("RemoveBtnTop", isVideos);
            SetVisibilityByName("SetDefaultBtnTop", isVideos);
            var fallbackLabel = FindDescendantByName<System.Windows.Controls.TextBlock>(this, "ViewLabelTop");
            if (fallbackLabel != null) fallbackLabel.Visibility = isVideos ? Visibility.Visible : Visibility.Collapsed;
            var fallbackCombo = FindDescendantByName<System.Windows.Controls.ComboBox>(this, "ViewModeComboTop");
            if (fallbackCombo != null) fallbackCombo.Visibility = isVideos ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        // clear and populate according to active page
        pageHost.Children.Clear();

        if (isVideos)
        {
            var add = new System.Windows.Controls.Button { Content = "+ Hinzufügen", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            add.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.AddVideosAsyncFromHost());
            pageHost.Children.Add(add);

            var remove = new System.Windows.Controls.Button { Content = "Entfernen", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            remove.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.RemoveSelectedFromHost());
            pageHost.Children.Add(remove);

            var setDefault = new System.Windows.Controls.Button { Content = "Als Default", Margin = new Thickness(0, 0, 8, 0), MinWidth = 80 };
            setDefault.Click += async (_, __) => await InvokeOnActiveVideosPageAsync(p => p.SetDefaultFromHost());
            pageHost.Children.Add(setDefault);

            // ensure view controls visible
            var vmCombo = FindDescendantByName<System.Windows.Controls.ComboBox>(this, "ViewModeComboTop");
            if (vmCombo != null) vmCombo.Visibility = Visibility.Visible;
            var viewLabel = FindDescendantByName<System.Windows.Controls.TextBlock>(this, "ViewLabelTop");
            if (viewLabel != null) viewLabel.Visibility = Visibility.Visible;

            pageHost.Visibility = Visibility.Visible;
            return;
        }

        if (isLogs)
        {
            var refresh = new System.Windows.Controls.Button { Content = "Refresh", Margin = new Thickness(0, 0, 8, 0) };
            refresh.Click += async (_, __) => await InvokeOnActiveLogsPageAsync(p => p.RefreshAsync());
            pageHost.Children.Add(refresh);

            var del = new System.Windows.Controls.Button { Content = "Löschen", Margin = new Thickness(0, 0, 8, 0) };
            del.Click += async (_, __) => await InvokeOnActiveLogsPageAsync(p => p.DeleteSelectedAsync());
            pageHost.Children.Add(del);

            var open = new System.Windows.Controls.Button { Content = "Ordner öffnen", Margin = new Thickness(0, 0, 8, 0) };
            open.Click += (_, __) => _ = InvokeOnActiveLogsPageAsync(p => { p.OpenFolder(); return Task.CompletedTask; });
            pageHost.Children.Add(open);

            // hide view controls
            var vmCombo = FindDescendantByName<System.Windows.Controls.ComboBox>(this, "ViewModeComboTop");
            if (vmCombo != null) vmCombo.Visibility = Visibility.Collapsed;
            var viewLabel = FindDescendantByName<System.Windows.Controls.TextBlock>(this, "ViewLabelTop");
            if (viewLabel != null) viewLabel.Visibility = Visibility.Collapsed;

            pageHost.Visibility = Visibility.Visible;
            return;
        }

        // nothing active
        pageHost.Visibility = Visibility.Collapsed;
    }

    // Hilfsfunktion: setzt Visibility eines FrameworkElementes nach Name (unabhängig vom konkreten Typ)
    private void SetVisibilityByName(string name, bool visible)
    {
        var fe = FindDescendantByName<FrameworkElement>(this, name);
        if (fe != null) fe.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public void RefreshBindings()
    {
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private async Task InvokeOnActiveVideosPageAsync(System.Func<Pages.VideosPage, Task> action)
    {
        if (Nav == null)
        {
            // Versuche trotzdem, Navigation auszuführen, nur auf Windows
            if (OperatingSystem.IsWindows())
                Nav?.Navigate(typeof(Pages.VideosPage)); // safe-call, Nav==null wird übersprungen
            return;
        }

        // Suche die Seite im Nav-VisualTree
        var page = FindDescendant<Pages.VideosPage>(Nav);
        if (page != null)
        {
            await action(page);
            UpdateTopButtonsVisibility();
            return;
        }

        // Falls nicht gefunden, navigiere (plattform-guard)
        if (OperatingSystem.IsWindows())
        {
            if (Nav.Navigate(typeof(Pages.VideosPage)))
            {
                await Task.Delay(150);
                page = FindDescendant<Pages.VideosPage>(Nav);
                if (page != null) { await action(page); UpdateTopButtonsVisibility(); }
            }
        }
    }

    private async Task InvokeOnActiveLogsPageAsync(System.Func<Pages.LogsPage, Task> action)
    {
        if (Nav == null)
        {
            // Versuche trotzdem, Navigation auszuführen, nur auf Windows
            if (OperatingSystem.IsWindows())
                Nav?.Navigate(typeof(Pages.LogsPage)); // safe-call, Nav==null wird übersprungen
            return;
        }

        // Suche die Seite im Nav-VisualTree
        var page = FindDescendant<Pages.LogsPage>(Nav);
        if (page != null)
        {
            await action(page);
            UpdateTopButtonsVisibility();
            return;
        }

        // Falls nicht gefunden, navigiere (plattform-guard)
        if (OperatingSystem.IsWindows())
        {
            if (Nav.Navigate(typeof(Pages.LogsPage)))
            {
                await Task.Delay(150);
                page = FindDescendant<Pages.LogsPage>(Nav);
                if (page != null) { await action(page); UpdateTopButtonsVisibility(); }
            }
        }
    }

    private void MainWindow_PreviewMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        // Nur ancestor scrollviewer nutzen — kein globaler Fallback mehr
        var dep = e.OriginalSource as DependencyObject ?? Mouse.DirectlyOver as DependencyObject;
        var sv = FindAncestorScrollViewer(dep);
        if (sv != null)
        {
            var newOffset = sv.VerticalOffset - e.Delta;
            if (newOffset < 0) newOffset = 0;
            if (newOffset > sv.ExtentHeight) newOffset = sv.ExtentHeight;
            sv.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }
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

    private T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) return t;
            var found = FindDescendant<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    // Hilfsfunktion: findet ein Element nach Namen zur Laufzeit (vermeidet Compile-Fehler, wenn XAML-Felder fehlen)
    private T? FindElement<T>(string name) where T : class
    {
        try
        {
            var obj = this.FindName(name) as T;
            if (obj != null) return obj;

            // Fallback: Suche im visuellen Baum
            return FindDescendantByName<T>(this, name);
        }
        catch
        {
            return null;
        }
    }

    private static T? FindDescendantByName<T>(DependencyObject root, string name) where T : class
    {
        if (root == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement fe && fe.Name == name && fe is T t) return t;
            var found = FindDescendantByName<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}