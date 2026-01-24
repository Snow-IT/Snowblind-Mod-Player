using System;
using System.IO;
using System.Text;

namespace SnowblindModPlayer
{
    public static class LogService
    {
        private static readonly object _sync = new();
        private static bool _verbose = false;
        private static string _logFile = "";

        public static string LogFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Snowblind-Mod Player",
                         "Logs");

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                // Extension auf .log geändert, damit LogsPage die Dateien findet
                _logFile = Path.Combine(LogFolder, $"log-{DateTime.UtcNow:yyyyMMdd}.log");
                LogInfo("LogService initialized.");
            }
            catch
            {
                // intentionally swallow - logging must not crash the app
            }
        }

        public static void SetVerbose(bool verbose)
        {
            _verbose = verbose;
            LogInfo($"Verbose logging set to {_verbose}.");
        }

        public static void LogInfo(string message)
        {
            Write("INFO", message);
        }

        public static void LogDebug(string message)
        {
            if (!_verbose) return;
            Write("DEBUG", message);
        }

        public static void LogError(string message)
        {
            Write("ERROR", message);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (_sync)
                {
                    if (string.IsNullOrEmpty(_logFile))
                    {
                        // fallback: try to initialize on first write
                        Directory.CreateDirectory(LogFolder);
                        _logFile = Path.Combine(LogFolder, $"log-{DateTime.UtcNow:yyyyMMdd}.log");
                    }

                    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                    File.AppendAllText(_logFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // never throw from logging
            }
        }
    }
}