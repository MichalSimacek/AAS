using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Services
{
    public class TranslationService
    {
        private readonly HttpClient _http;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _cfg;

        public TranslationService(HttpClient http, IServiceProvider serviceProvider, IConfiguration cfg)
        {
            _http = http;
            _serviceProvider = serviceProvider;
            _cfg = cfg;
        }

        record LtReq(string q, string source, string target, string? api_key);
        record LtRes(string translatedText);

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase)) return text;

            var enabled = bool.Parse(Environment.GetEnvironmentVariable("TRANSLATION_ENABLED") ?? _cfg["Translation:Enabled"] ?? "false");
            if (!enabled) return text;

            var hash = Hash(text + "|" + sourceLang + "|" + targetLang);

            // CRITICAL FIX: Use scoped DbContext to avoid memory leak
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cached = await db.TranslationCaches.AsNoTracking().FirstOrDefaultAsync(x => x.SourceHash == hash);
            if (cached != null) return cached.TranslatedText;

            var ep = Environment.GetEnvironmentVariable("TRANSLATION_ENDPOINT") ?? _cfg["Translation:Endpoint"];
            if (string.IsNullOrWhiteSpace(ep)) return text;

            try
            {
                var apiKey = Environment.GetEnvironmentVariable("TRANSLATION_API_KEY") ?? _cfg["Translation:ApiKey"];
                var req = new LtReq(text, sourceLang, targetLang, string.IsNullOrWhiteSpace(apiKey) ? null : apiKey);
                var res = await _http.PostAsJsonAsync(ep, req);
                res.EnsureSuccessStatusCode();
                var dto = await res.Content.ReadFromJsonAsync<LtRes>();
                var tr = new TranslationCache { SourceHash = hash, SourceLang = sourceLang, TargetLang = targetLang, SourceText = text, TranslatedText = dto?.translatedText ?? text };
                db.Add(tr);
                await db.SaveChangesAsync();
                return tr.TranslatedText;
            }
            catch { return text; }
        }

        static string Hash(string s) { using var sha = SHA256.Create(); return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))); }
    }
}