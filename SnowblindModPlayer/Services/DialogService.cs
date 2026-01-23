using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace SnowblindModPlayer;

public static class DialogService
{
    private static ContentPresenter? _host;

    public static void Initialize(ContentPresenter host)
    {
        _host = host;
    }

    public static async Task ShowMessageAsync(string title, string message)
    {
        if (_host == null)
            throw new InvalidOperationException("DialogService not initialized.");

        var dialog = new ContentDialog(_host)
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync(CancellationToken.None);
    }

    public static async Task<bool> ConfirmAsync(string title, string message, string primary = "OK", string close = "Abbrechen")
    {
        if (_host == null)
            throw new InvalidOperationException("DialogService not initialized.");

        var dialog = new ContentDialog(_host)
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primary,
            CloseButtonText = close
        };

        var result = await dialog.ShowAsync(CancellationToken.None);
        return result == ContentDialogResult.Primary;
    }

    // Lightweight feedback. Later we can swap this to Snackbar.
    public static Task ToastAsync(string message)
        => ShowMessageAsync("", message);
}
