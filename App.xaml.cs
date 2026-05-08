using System;
using System.Windows;
using Microsoft.Win32;

namespace UnifiedExplorer
{
    public partial class App : Application
    {
        public static AppSettings Settings { get; set; } = new AppSettings();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Settings = SettingsManager.LoadSettings();
            ApplyTheme(Settings.Theme);

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && Settings.Theme == ThemeMode.System)
            {
                ApplyTheme(ThemeMode.System);
            }
        }

        public static void ApplyTheme(ThemeMode mode)
        {
            Settings.Theme = mode;
            SettingsManager.SaveSettings(Settings);

            bool isLight = true;
            if (mode == ThemeMode.Dark)
            {
                isLight = false;
            }
            else if (mode == ThemeMode.System)
            {
                isLight = SettingsManager.IsSystemThemeLight();
            }

            var dict = new ResourceDictionary();
            dict.Source = isLight 
                ? new Uri("Themes/LightMode.xaml", UriKind.Relative)
                : new Uri("Themes/DarkMode.xaml", UriKind.Relative);

            Current.Resources.MergedDictionaries.Clear();
            Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
