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
        
        // DeepL API limits: Free API max 128KB, but we use very conservative chunks
        // With HTML tags, 10KB text can easily become 50KB+ request
        private const int MAX_CHUNK_SIZE = 10000; // characters (conservative for HTML content)

        // Language codes mapping (DeepL uses different codes)
        // Note: DeepL doesn't support Hindi (hi), so it will fallback to English translation
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

            var containsHtml = text.Contains("<") && text.Contains(">");
            _logger.LogInformation($"DeepL TranslateAsync called: source={sourceLang}, target={targetLang}, textLength={text.Length}, containsHtml={containsHtml}");

            // If text is too long, split into chunks and translate separately
            if (text.Length > MAX_CHUNK_SIZE)
            {
                _logger.LogInformation($"Text is too long ({text.Length} chars), splitting into chunks...");
                return await TranslateInChunksAsync(text, targetLang, sourceLang);
            }

            try
            {
                // Map language codes
                var targetLangCode = _langMap.ContainsKey(targetLang) ? _langMap[targetLang] : targetLang.ToUpper();
                
                // Handle source language mapping
                string sourceLangCode;
                if (sourceLang == "auto")
                {
                    sourceLangCode = "auto"; // DeepL automatic detection
                }
                else if (sourceLang == "cs")
                {
                    sourceLangCode = "CS"; // Czech
                }
                else if (_langMap.ContainsKey(sourceLang))
                {
                    sourceLangCode = _langMap[sourceLang];
                }
                else
                {
                    sourceLangCode = sourceLang.ToUpper();
                }

                _logger.LogInformation($"Mapped codes: source={sourceLangCode}, target={targetLangCode}");

                var requestData = new Dictionary<string, string>
                {
                    { "text", text },
                    { "target_lang", targetLangCode },
                    { "tag_handling", "html" }, // Enable HTML tag handling
                    { "split_sentences", "nonewlines" } // Preserve paragraph structure
                };

                if (sourceLangCode != "auto")
                {
                    requestData.Add("source_lang", sourceLangCode);
                    _logger.LogInformation($"Including source_lang={sourceLangCode} in request");
                }
                else
                {
                    _logger.LogInformation("Using automatic language detection (no source_lang specified)");
                }

                var content = new FormUrlEncodedContent(requestData);
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");
                request.Content = content;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)); // Increased timeout for long content
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

            // Include all target languages, even if not in _langMap (for Hindi fallback)
            var allLanguages = new[] { "en", "de", "es", "fr", "hi", "ja", "pt", "ru", "zh" };

            // First, translate to English if needed (for Hindi fallback)
            string? englishTranslation = null;

            foreach (var lang in allLanguages)
            {
                try
                {
                    // Skip if same as source
                    if (lang == sourceLang)
                    {
                        translations[lang] = text;
                        continue;
                    }

                    // Check if DeepL supports this language
                    if (!_langMap.ContainsKey(lang))
                    {
                        // For Hindi: fallback to English translation
                        if (lang == "hi")
                        {
                            if (englishTranslation == null && sourceLang != "en")
                            {
                                _logger.LogInformation("Translating to English for Hindi fallback...");
                                englishTranslation = await TranslateAsync(text, "en", sourceLang);
                            }
                            translations[lang] = englishTranslation ?? text;
                            _logger.LogWarning($"DeepL doesn't support {lang}, using English translation as fallback");
                        }
                        else
                        {
                            _logger.LogWarning($"DeepL doesn't support {lang}, using original text");
                            translations[lang] = text;
                        }
                        continue;
                    }

                    var translated = await TranslateAsync(text, lang, sourceLang);
                    translations[lang] = translated;
                    
                    // Store English translation for potential Hindi fallback
                    if (lang == "en")
                    {
                        englishTranslation = translated;
                    }
                    
                    // Small delay to avoid rate limiting
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to translate to {lang}");
                    // For Hindi, try to use English as fallback even on error
                    if (lang == "hi" && englishTranslation != null)
                    {
                        translations[lang] = englishTranslation;
                    }
                    else
                    {
                        translations[lang] = text; // Fallback to original
                    }
                }
            }

            return translations;
        }

        private async Task<string> TranslateInChunksAsync(string text, string targetLang, string sourceLang)
        {
            try
            {
                var chunks = SplitIntoChunks(text, MAX_CHUNK_SIZE);
                _logger.LogInformation($"Split text into {chunks.Count} chunks for translation");

                var translatedChunks = new List<string>();
                
                for (int i = 0; i < chunks.Count; i++)
                {
                    _logger.LogInformation($"Translating chunk {i + 1}/{chunks.Count} (length: {chunks[i].Length})");
                    
                    // Translate single chunk using existing logic (but recursion-safe since chunk is smaller)
                    var originalLength = text.Length;
                    text = chunks[i]; // Temporarily replace text
                    
                    var translatedChunk = await TranslateSingleChunkAsync(chunks[i], targetLang, sourceLang);
                    translatedChunks.Add(translatedChunk);
                    
                    text = string.Join("", chunks); // Restore for next iteration
                    
                    // Small delay to avoid rate limiting
                    if (i < chunks.Count - 1)
                    {
                        await Task.Delay(200);
                    }
                }

                var result = string.Join("", translatedChunks);
                _logger.LogInformation($"Successfully translated all chunks. Total length: {result.Length}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to translate text in chunks");
                return text; // Return original on error
            }
        }

        private async Task<string> TranslateSingleChunkAsync(string text, string targetLang, string sourceLang)
        {
            // This is the core translation logic without chunking
            try
            {
                var targetLangCode = _langMap.ContainsKey(targetLang) ? _langMap[targetLang] : targetLang.ToUpper();
                
                string sourceLangCode;
                if (sourceLang == "auto")
                {
                    sourceLangCode = "auto";
                }
                else if (sourceLang == "cs")
                {
                    sourceLangCode = "CS";
                }
                else if (_langMap.ContainsKey(sourceLang))
                {
                    sourceLangCode = _langMap[sourceLang];
                }
                else
                {
                    sourceLangCode = sourceLang.ToUpper();
                }

                var requestData = new Dictionary<string, string>
                {
                    { "text", text },
                    { "target_lang", targetLangCode },
                    { "tag_handling", "html" },
                    { "split_sentences", "nonewlines" }
                };

                if (sourceLangCode != "auto")
                {
                    requestData.Add("source_lang", sourceLangCode);
                }

                var content = new FormUrlEncodedContent(requestData);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");
                request.Content = content;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                
                var translations = jsonDoc.RootElement.GetProperty("translations");
                if (translations.GetArrayLength() > 0)
                {
                    return translations[0].GetProperty("text").GetString() ?? text;
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DeepL chunk translation failed");
                return text;
            }
        }

        private List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            
            if (text.Length <= maxChunkSize)
            {
                chunks.Add(text);
                return chunks;
            }

            // For HTML content, try to split at paragraph boundaries to preserve structure
            var containsHtml = text.Contains("<p>") || text.Contains("</p>");
            
            if (containsHtml)
            {
                // Split by paragraph tags
                var paragraphs = text.Split(new[] { "</p>" }, StringSplitOptions.None);
                var currentChunk = new StringBuilder();
                
                foreach (var para in paragraphs)
                {
                    var paraWithTag = para + (para == paragraphs.Last() ? "" : "</p>");
                    
                    if (currentChunk.Length + paraWithTag.Length > maxChunkSize && currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString());
                        currentChunk.Clear();
                    }
                    
                    currentChunk.Append(paraWithTag);
                }
                
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                }
            }
            else
            {
                // Simple character-based splitting for non-HTML
                for (int i = 0; i < text.Length; i += maxChunkSize)
                {
                    var length = Math.Min(maxChunkSize, text.Length - i);
                    chunks.Add(text.Substring(i, length));
                }
            }

            return chunks;
        }
    }
}
