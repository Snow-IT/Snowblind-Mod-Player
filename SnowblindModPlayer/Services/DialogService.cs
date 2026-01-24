using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace SnowblindModPlayer
{
    public static class DialogService
    {
        private static ContentDialogHost? _host;

        public static void Initialize(ContentDialogHost host)
        {
            _host = host;
        }

        public static async Task ShowMessageAsync(string title, string message)
        {
            if (_host == null)
                throw new InvalidOperationException("DialogService not initialized with a ContentDialogHost.");

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
                throw new InvalidOperationException("DialogService not initialized with a ContentDialogHost.");

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

        // Toast: bevorzugt MainWindow.SetHint (transient), Fallback auf modal ShowMessageAsync
        public static Task ToastAsync(string message)
        {
            try
            {
                var main = System.Windows.Application.Current?.MainWindow as MainWindow;
                if (main != null)
                {
                    main.SetHint(message, 1500);
                    return Task.CompletedTask;
                }
            }
            catch
            {
                // ignore and fallback
            }

            return ShowMessageAsync("", message);
        }

        internal static void Initialize(ContentPresenter contentPresenter)
        {
            throw new NotImplementedException();
        }
    }
}
