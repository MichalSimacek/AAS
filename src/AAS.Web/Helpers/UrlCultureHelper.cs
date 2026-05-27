using System.Globalization;

namespace AAS.Web.Helpers
{
    /// <summary>
    /// Helper for variant-C localized URLs (culture prefix in path).
    /// English is the default culture and uses NO prefix; all other supported
    /// cultures use a "/{culture}" prefix (e.g. "/ru", "/cs").
    /// </summary>
    public static class UrlCultureHelper
    {
        private static readonly HashSet<string> NonDefault = new(StringComparer.OrdinalIgnoreCase)
        {
            "cs", "ru", "de", "es", "fr", "zh", "pt", "hi", "ja"
        };

        /// <summary>"" for English, "/cs" / "/ru" / ... otherwise.</summary>
        public static string CulturePrefix(string? culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return NonDefault.Contains(culture) ? "/" + culture.ToLowerInvariant() : string.Empty;
        }

        /// <summary>
        /// Build a localized URL by prefixing the active culture (if non-default)
        /// to a leading-slash app path. Use for internal navigation.
        /// </summary>
        public static string Loc(string path, string? culture = null)
        {
            if (string.IsNullOrEmpty(path)) path = "/";
            if (!path.StartsWith('/')) path = "/" + path;
            var prefix = CulturePrefix(culture);
            if (string.IsNullOrEmpty(prefix)) return path;
            return path == "/" ? prefix : prefix + path;
        }
    }
}
