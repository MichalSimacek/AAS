using System.Text;
using System.Text.Json;

namespace AAS.Web.Services
{
    public interface IDeepLService
    {
        Task<string> TranslateAsync(string text, string targetLang, string sourceLang = "auto");
        Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLang = "cs");
    }

    public class DeepLService : IDeepLService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<DeepLService> _logger;

        // Language codes mapping (DeepL uses different codes)
        // Note: DeepL doesn't support Hindi (hi), so it will fallback to original text
        private readonly Dictionary<string, string> _langMap = new()
        {
            { "en", "EN-US" },
            { "de", "DE" },
            { "es", "ES" },
            { "fr", "FR" },
            { "ja", "JA" },
            { "pt", "PT-PT" },
            { "ru", "RU" },
            { "zh", "ZH" }
        };

        public DeepLService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DeepLService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["DEEPL_API_KEY"] ?? throw new InvalidOperationException("DEEPL_API_KEY not configured");
            _logger = logger;
        }

        public async Task<string> TranslateAsync(string text, string targetLang, string sourceLang = "auto")
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                // Map language codes
                var targetLangCode = _langMap.ContainsKey(targetLang) ? _langMap[targetLang] : targetLang.ToUpper();
                var sourceLangCode = sourceLang == "auto" ? "auto" : (sourceLang == "cs" ? "CS" : sourceLang.ToUpper());

                var requestData = new Dictionary<string, string>
                {
                    { "text", text },
                    { "target_lang", targetLangCode }
                };

                if (sourceLangCode != "auto")
                {
                    requestData.Add("source_lang", sourceLangCode);
                }

                var content = new FormUrlEncodedContent(requestData);
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");
                request.Content = content;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                
                var translations = jsonDoc.RootElement.GetProperty("translations");
                if (translations.GetArrayLength() > 0)
                {
                    var translatedText = translations[0].GetProperty("text").GetString();
                    _logger.LogInformation($"Translated to {targetLang}: {text.Substring(0, Math.Min(50, text.Length))}... -> {translatedText?.Substring(0, Math.Min(50, translatedText.Length))}...");
                    return translatedText ?? text;
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DeepL translation failed for target language {targetLang}");
                return text; // Return original text on error
            }
        }

        public async Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLang = "cs")
        {
            var translations = new Dictionary<string, string>();

            foreach (var lang in _langMap.Keys)
            {
                try
                {
                    var translated = await TranslateAsync(text, lang, sourceLang);
                    translations[lang] = translated;
                    
                    // Small delay to avoid rate limiting
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to translate to {lang}");
                    translations[lang] = text; // Fallback to original
                }
            }

            return translations;
        }
    }
}
