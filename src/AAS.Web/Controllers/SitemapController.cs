using AAS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AAS.Web.Controllers
{
    [Route("sitemap.xml")]
    public class SitemapController : Controller
    {
        private readonly AppDbContext _db; public SitemapController(AppDbContext db) { _db = db; }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            void Url(string path) { sb.AppendLine($"<url><loc>{baseUrl}{path}</loc></url>"); }
            Url("/"); Url("/Collections"); Url("/About"); Url("/Contacts");
            var slugs = await _db.Collections.Select(c => c.Slug).ToListAsync();
            foreach (var s in slugs) Url($"/collections/{s}");
            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}