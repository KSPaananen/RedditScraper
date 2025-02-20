using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RedditScraper.AppSettings
{
    internal class SettingsManager
    {
        public static Settings ReadSettings()
        {
            // Get filepath to settings file
            var filepath = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}", "settings.json", SearchOption.AllDirectories).FirstOrDefault();

            if (filepath == null)
                return new Settings();

            // Read json string
            string jsonString = File.ReadAllText(filepath);

            // Convert to object
            Settings settings = JsonSerializer.Deserialize<Settings>(jsonString)!;

            return settings;
        }

        public static void SaveSettings(Settings settings)
        {
            // Get filepath
            var filepath = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}", "settings.json", SearchOption.AllDirectories).FirstOrDefault();

            if (filepath == null)
                return;

            // Convert to jsonstring
            string jsonString = JsonSerializer.Serialize<Settings>(settings);

            // Write to file
            try
            {
                File.WriteAllText(filepath, jsonString);
            }
            catch
            {

            }

        }
    }
}
