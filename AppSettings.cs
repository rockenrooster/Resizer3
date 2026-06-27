using System.Text.Json;

namespace Resizer3
{
    internal class AppSettings
    {
        public string SaveLocation { get; set; } = @"C:\img";
        public decimal ThreadsNumber { get; set; }
        public decimal Resolution { get; set; } = 100;
        public decimal Quality { get; set; } = 95;
        public string? Format { get; set; }
        public string? MaxRes { get; set; } = "Off";
        public int WebpEffort { get; set; } = 2;

        private static string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Resizer3", "settings.json");

        public static AppSettings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public bool TrySave()
        {
            string tmpPath = SettingsFilePath + ".tmp";
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);

                File.WriteAllText(tmpPath, json);
                File.Move(tmpPath, SettingsFilePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                AppLog.TryLog("AppSettings.TrySave", ex);
                try
                {
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
                catch { }
                return false;
            }
        }
    }
}
