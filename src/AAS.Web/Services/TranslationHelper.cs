using System.Globalization;
using System.Text.Json;

namespace AAS.Web.Services
{
    public class TranslationHelper
    {
        private static readonly Dictionary<string, Dictionary<string, string>> CategoryTranslations = new()
        {
            ["Paintings"] = new() {
                ["cs"] = "Obrazy",
                ["ru"] = "Картины",
                ["de"] = "Gemälde",
                ["es"] = "Pinturas",
                ["fr"] = "Peintures",
                ["zh"] = "绘画",
                ["pt"] = "Pinturas",
                ["hi"] = "चित्रकारी",
                ["ja"] = "絵画"
            },
            ["Jewelry"] = new() {
                ["cs"] = "Šperky",
                ["ru"] = "Ювелирные изделия",
                ["de"] = "Schmuck",
                ["es"] = "Joyería",
                ["fr"] = "Bijoux",
                ["zh"] = "珠宝",
                ["pt"] = "Joias",
                ["hi"] = "आभूषण",
                ["ja"] = "ジュエリー"
            },
            ["Watches"] = new() {
                ["cs"] = "Hodinky",
                ["ru"] = "Часы",
                ["de"] = "Uhren",
                ["es"] = "Relojes",
                ["fr"] = "Montres",
                ["zh"] = "腕表",
                ["pt"] = "Relógios",
                ["hi"] = "घड़ियां",
                ["ja"] = "時計"
            },
            ["Statues"] = new() {
                ["cs"] = "Sochy",
                ["ru"] = "Статуи",
                ["de"] = "Statuen",
                ["es"] = "Estatuas",
                ["fr"] = "Statues",
                ["zh"] = "雕像",
                ["pt"] = "Estátuas",
                ["hi"] = "मूर्तियां",
                ["ja"] = "彫像"
            },
            ["Other"] = new() {
                ["cs"] = "Ostatní",
                ["ru"] = "Другое",
                ["de"] = "Andere",
                ["es"] = "Otros",
                ["fr"] = "Autre",
                ["zh"] = "其他",
                ["pt"] = "Outros",
                ["hi"] = "अन्य",
                ["ja"] = "その他"
            }
        };

        public static string TranslateCategory(string category, string? culture = null)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            }

            if (culture == "en")
            {
                return category;
            }

            if (CategoryTranslations.ContainsKey(category) && 
                CategoryTranslations[category].ContainsKey(culture))
            {
                return CategoryTranslations[category][culture];
            }

            return category;
        }
    }
}
