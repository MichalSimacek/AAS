using AAS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AAS.Web.Controllers
{
    [Route("sitemap.xml")]
    public class SitemapController : Controller
    {
        // EN is the default language and has NO URL prefix.
        private const string DefaultCulture = "en";
        // Non-default cultures use a path prefix (e.g. /cs/, /ru/, /de/).
        private static readonly string[] NonDefaultCultures = { "cs", "ru", "de", "es", "fr", "zh", "pt", "hi", "ja" };
        private static readonly string[] AllCultures =
            new[] { DefaultCulture }.Concat(NonDefaultCultures).ToArray();
        private static readonly Dictionary<string, string> Bcp47 = new()
        {
            {"en","en"}, {"cs","cs-CZ"}, {"ru","ru-RU"}, {"de","de-DE"},
            {"es","es-ES"}, {"fr","fr-FR"}, {"zh","zh-CN"}, {"pt","pt-PT"},
            {"hi","hi-IN"}, {"ja","ja-JP"}
        };

        private readonly AppDbContext _db;
        public SitemapController(AppDbContext db) { _db = db; }

        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Get()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" ");
            sb.Append("xmlns:xhtml=\"http://www.w3.org/1999/xhtml\" ");
            sb.AppendLine("xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");

            string Esc(string s) => System.Security.SecurityElement.Escape(s) ?? "";
            static string Prefix(string c) => c == DefaultCulture ? "" : $"/{c}";

            // Emit one <url> per culture with proper hreflang alternates between them.
            // We pass two callbacks:
            //   pathFor(culture)        → e.g. "/Collections/Details/{slug}"     (no leading culture)
            //   slugForCollection(c,id) → optional per-culture variant of the slug (we use EN slug for non-CS)
            void AppendUrlSet(Func<string, string?> basePathFor, DateTime? lastmod, string changefreq,
                              string priority, IEnumerable<string>? images = null)
            {
                foreach (var c in AllCultures)
                {
                    var bp = basePathFor(c);
                    if (string.IsNullOrEmpty(bp)) continue;          // skip cultures lacking a slug
                    var fullPath = Prefix(c) + bp;

                    sb.AppendLine("  <url>");
                    sb.AppendLine($"    <loc>{baseUrl}{fullPath}</loc>");
                    if (lastmod.HasValue)
                        sb.AppendLine($"    <lastmod>{lastmod.Value.ToUniversalTime():yyyy-MM-dd}</lastmod>");
                    sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                    sb.AppendLine($"    <priority>{priority}</priority>");

                    foreach (var alt in AllCultures)
                    {
                        var altBp = basePathFor(alt);
                        if (string.IsNullOrEmpty(altBp)) continue;
                        var altPath = Prefix(alt) + altBp;
                        var tag = Bcp47.TryGetValue(alt, out var t) ? t : alt;
                        sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"{tag}\" href=\"{baseUrl}{altPath}\" />");
                    }
                    // x-default → English version
                    var defaultBp = basePathFor(DefaultCulture);
                    if (!string.IsNullOrEmpty(defaultBp))
                        sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{baseUrl}{defaultBp}\" />");

                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            sb.AppendLine("    <image:image>");
                            sb.AppendLine($"      <image:loc>{Esc(img)}</image:loc>");
                            sb.AppendLine("    </image:image>");
                        }
                    }
                    sb.AppendLine("  </url>");
                }
            }

            // Static pages — same path in every language, only prefix differs.
            AppendUrlSet(_ => "/",                    DateTime.UtcNow, "weekly",  "1.0");
            AppendUrlSet(_ => "/Collections/Landing", DateTime.UtcNow, "daily",   "0.9");
            AppendUrlSet(_ => "/Collections",         DateTime.UtcNow, "daily",   "0.9");
            AppendUrlSet(_ => "/Blog",                DateTime.UtcNow, "weekly",  "0.7");
            AppendUrlSet(_ => "/About",               null,            "monthly", "0.6");
            AppendUrlSet(_ => "/Contacts",            null,            "monthly", "0.6");
            AppendUrlSet(_ => "/HowTo",               null,            "monthly", "0.5");

            // Collection detail pages — slug is language-specific.
            //   For CS:  use the original Slug column
            //   For all other (incl. EN default): use SlugEn (falls back to Slug if SlugEn is null)
            var collections = await _db.Collections
                .Where(c => !c.IsHidden)
                .Select(c => new
                {
                    c.Slug,
                    c.SlugEn,
                    c.CreatedUtc,
                    Images = c.Images.OrderBy(i => i.SortOrder).Take(3).Select(i => i.FileName).ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var c in collections)
            {
                var imgs = c.Images.Select(fn => $"{baseUrl}/uploads/images/{fn}-1600.jpg");
                var enSlug = !string.IsNullOrEmpty(c.SlugEn) ? c.SlugEn : c.Slug;
                AppendUrlSet(culture => culture == "cs"
                                ? $"/Collections/Details/{c.Slug}"
                                : $"/Collections/Details/{enSlug}",
                             c.CreatedUtc, "weekly", "0.8", imgs);
            }

            // Blog posts — only published, same path in every language.
            var posts = await _db.BlogPosts
                .Where(p => p.Published)
                .Select(p => new { p.Id, p.UpdatedAt, p.CreatedAt })
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in posts)
            {
                var lm = p.UpdatedAt ?? p.CreatedAt;
                AppendUrlSet(_ => $"/Blog/Post/{p.Id}", lm, "monthly", "0.6");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
