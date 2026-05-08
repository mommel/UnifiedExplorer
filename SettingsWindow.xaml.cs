using System.Windows;

namespace UnifiedExplorer
{
    public partial class SettingsWindow : Window
    {
        private bool _isInitialized = false;

        public SettingsWindow()
        {
            InitializeComponent();
            
            // Set initial state based on current settings
            switch (App.Settings.Theme)
            {
                case ThemeMode.Light:
                    LightThemeRadio.IsChecked = true;
                    break;
                case ThemeMode.Dark:
                    DarkThemeRadio.IsChecked = true;
                    break;
                case ThemeMode.System:
                    SystemThemeRadio.IsChecked = true;
                    break;
            }
            
            _isInitialized = true;
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            if (DarkThemeRadio.IsChecked == true)
            {
                App.ApplyTheme(ThemeMode.Dark);
            }
            else if (LightThemeRadio.IsChecked == true)
            {
                App.ApplyTheme(ThemeMode.Light);
            }
            else if (SystemThemeRadio.IsChecked == true)
            {
                App.ApplyTheme(ThemeMode.System);
            }
        }
    }
}
