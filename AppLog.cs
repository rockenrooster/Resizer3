using System;
using System.IO;

namespace Resizer3
{
    internal static class AppLog
    {
        private static string LogDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Resizer3");

        private static string LogPath => Path.Combine(LogDir, "Resizer3.log");

        internal static void TryLog(string context, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var line =
                    $"{DateTime.UtcNow:O} | {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}

