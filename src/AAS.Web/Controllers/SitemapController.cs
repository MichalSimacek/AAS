using AAS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml;

namespace AAS.Web.Controllers
{
    [Route("sitemap.xml")]
    public class SitemapController : Controller
    {
        private static readonly string[] Cultures = { "en", "cs", "ru", "de", "es", "fr", "zh", "pt", "hi", "ja" };
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

            void AppendUrl(string path, DateTime? lastmod, string changefreq, string priority,
                           IEnumerable<string>? images = null)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
                if (lastmod.HasValue)
                    sb.AppendLine($"    <lastmod>{lastmod.Value.ToUniversalTime():yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                foreach (var c in Cultures)
                {
                    var tag = Bcp47.TryGetValue(c, out var t) ? t : c;
                    var sep = path.Contains('?') ? "&amp;" : "?";
                    sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"{tag}\" href=\"{baseUrl}{path}{sep}culture={c}\" />");
                }
                sb.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{baseUrl}{path}\" />");
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

            // Static pages
            AppendUrl("/",                    DateTime.UtcNow, "weekly",  "1.0");
            AppendUrl("/Collections/Landing", DateTime.UtcNow, "daily",   "0.9");
            AppendUrl("/Collections",         DateTime.UtcNow, "daily",   "0.9");
            AppendUrl("/Blog",                DateTime.UtcNow, "weekly",  "0.7");
            AppendUrl("/About",               null,            "monthly", "0.6");
            AppendUrl("/Contacts",            null,            "monthly", "0.6");
            AppendUrl("/HowTo",               null,            "monthly", "0.5");

            // Collection detail pages — only visible ones, with lastmod from CreatedUtc
            // and (up to) the first 3 images per collection for Google Image Search.
            var collections = await _db.Collections
                .Where(c => !c.IsHidden)
                .Select(c => new
                {
                    c.Slug,
                    c.CreatedUtc,
                    Images = c.Images.OrderBy(i => i.SortOrder).Take(3).Select(i => i.FileName).ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var c in collections)
            {
                var imgs = c.Images.Select(fn => $"{baseUrl}/uploads/images/{fn}-1600.jpg");
                AppendUrl($"/Collections/Details/{c.Slug}", c.CreatedUtc, "weekly", "0.8", imgs);
            }

            // Blog posts — only published
            var posts = await _db.BlogPosts
                .Where(p => p.Published)
                .Select(p => new { p.Id, p.UpdatedAt, p.CreatedAt })
                .AsNoTracking()
                .ToListAsync();

            foreach (var p in posts)
            {
                var lm = p.UpdatedAt ?? p.CreatedAt;
                AppendUrl($"/Blog/Post/{p.Id}", lm, "monthly", "0.6");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
