using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace UnifiedExplorer
{
    public enum ThemeMode
    {
        Light,
        Dark,
        System
    }

    public class AppSettings
    {
        public ThemeMode Theme { get; set; } = ThemeMode.System;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static AppSettings LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
                catch { }
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        public static bool IsSystemThemeLight()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int i)
                    {
                        return i == 1;
                    }
                }
            }
            catch { }
            
            return true;
        }
    }
}
