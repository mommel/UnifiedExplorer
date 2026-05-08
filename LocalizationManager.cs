using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace UnifiedExplorer
{
    public static class LocalizationManager
    {
        private static bool IsGerman => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "de";

        private static readonly Dictionary<string, string> EnglishStrings = new Dictionary<string, string>
        {
            {"Name", "Name"},
            {"DateModified", "Date Modified"},
            {"Type", "Type"},
            {"Size", "Size"},
            {"CreationDate", "Creation Date"},
            {"Dimensions", "Dimensions"},
            {"SizeAllColumns", "Size all columns to fit"},
            {"Search", "Search..."},
            {"ThisPC", "This PC"},
            {"QuickAccess", "Quick access"},
            {"Desktop", "Desktop"},
            {"Downloads", "Downloads"},
            {"Documents", "Documents"},
            {"Pictures", "Pictures"},
            {"Music", "Music"},
            {"Videos", "Videos"},
            {"Loading", "Loading..."},
            {"FileFolder", "File folder"}
        };

        private static readonly Dictionary<string, string> GermanStrings = new Dictionary<string, string>
        {
            {"Name", "Name"},
            {"DateModified", "Änderungsdatum"},
            {"Type", "Typ"},
            {"Size", "Größe"},
            {"CreationDate", "Erstelldatum"},
            {"Dimensions", "Abmessungen"},
            {"SizeAllColumns", "Größe aller Spalten anpassen"},
            {"Search", "Suchen..."},
            {"ThisPC", "Dieser PC"},
            {"QuickAccess", "Schnellzugriff"},
            {"Desktop", "Desktop"},
            {"Downloads", "Downloads"},
            {"Documents", "Dokumente"},
            {"Pictures", "Bilder"},
            {"Music", "Musik"},
            {"Videos", "Videos"},
            {"Loading", "Wird geladen..."},
            {"FileFolder", "Dateiordner"}
        };

        public static string GetString(string key)
        {
            var dict = IsGerman ? GermanStrings : EnglishStrings;
            if (dict.TryGetValue(key, out string? value) && value != null)
                return value;
            return key; // Fallback to key if not found
        }
    }
}
