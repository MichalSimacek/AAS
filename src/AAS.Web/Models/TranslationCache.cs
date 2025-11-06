namespace AAS.Web.Models
{
    public class TranslationCache
    {
        public long Id { get; set; }
        public string SourceHash { get; set; } = string.Empty; // SHA256(text+src+dst)
        public string SourceLang { get; set; } = "en";
        public string TargetLang { get; set; } = "en";
        public string SourceText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}