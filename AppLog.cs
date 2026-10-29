using System;
using System.IO;

namespace Resizer3
{
    internal static class AppLog
    {
        // When the log passes this size it becomes Resizer3.old.log, so it never grows without bound.
        private const long MaxLogBytes = 1024 * 1024;

        private static readonly object Gate = new();

        private static string LogDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Resizer3");

        private static string LogPath => Path.Combine(LogDir, "Resizer3.log");

        internal static void TryLog(string context, Exception ex)
        {
            try
            {
                var line =
                    $"{DateTime.UtcNow:O} | {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
                lock (Gate)
                {
                    Directory.CreateDirectory(LogDir);
                    var info = new FileInfo(LogPath);
                    if (info.Exists && info.Length > MaxLogBytes)
                        File.Move(LogPath, Path.Combine(LogDir, "Resizer3.old.log"), overwrite: true);
                    File.AppendAllText(LogPath, line);
                }
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}
