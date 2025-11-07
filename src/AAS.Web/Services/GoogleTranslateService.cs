using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AAS.Web.Services
{
    public class GoogleTranslateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private static readonly Dictionary<string, string> _cache = new();

        public GoogleTranslateService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || targetLanguage == "en")
            {
                return text;
            }

            // Check cache first
            var cacheKey = $"{text}_{targetLanguage}";
            if (_cache.ContainsKey(cacheKey))
            {
                return _cache[cacheKey];
            }

            try
            {
                // Get API key from configuration
                var apiKey = _configuration["GoogleTranslateApiKey"];
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    // If no API key, return original text
                    return text;
                }

                var client = _httpClientFactory.CreateClient();
                var url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";

                var requestBody = new
                {
                    q = text,
                    target = targetLanguage,
                    source = "en",
                    format = "text"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleTranslateResponse>(jsonResponse);
                    
                    if (result?.data?.translations?.Count > 0)
                    {
                        var translatedText = result.data.translations[0].translatedText;
                        _cache[cacheKey] = translatedText;
                        return translatedText;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
            }

            return text;
        }

        private class GoogleTranslateResponse
        {
            public DataSection? data { get; set; }
        }

        private class DataSection
        {
            public List<Translation>? translations { get; set; }
        }

        private class Translation
        {
            public string translatedText { get; set; } = string.Empty;
        }
    }
}
