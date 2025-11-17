using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AAS.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BlogController> _logger;

        public BlogController(AppDbContext db, ILogger<BlogController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /Blog
        public async Task<IActionResult> Index()
        {
            var posts = await _db.BlogPosts
                .Where(p => p.Published)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        // GET: /Blog/Post/5
        [Route("Blog/Post/{id}")]
        public async Task<IActionResult> Post(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            
            if (post == null || !post.Published)
            {
                return NotFound();
            }

            return View(post);
        }

        // Helper method to get localized title
        public static string GetLocalizedTitle(BlogPost post, string culture)
        {
            return culture switch
            {
                "en" => post.TitleEn ?? post.TitleCs,
                "de" => post.TitleDe ?? post.TitleCs,
                "es" => post.TitleEs ?? post.TitleCs,
                "fr" => post.TitleFr ?? post.TitleCs,
                "hi" => post.TitleHi ?? post.TitleCs,
                "ja" => post.TitleJa ?? post.TitleCs,
                "pt" => post.TitlePt ?? post.TitleCs,
                "ru" => post.TitleRu ?? post.TitleCs,
                "zh" => post.TitleZh ?? post.TitleCs,
                _ => post.TitleCs
            };
        }

        // Helper method to get localized content
        public static string GetLocalizedContent(BlogPost post, string culture)
        {
            return culture switch
            {
                "en" => post.ContentEn ?? post.ContentCs,
                "de" => post.ContentDe ?? post.ContentCs,
                "es" => post.ContentEs ?? post.ContentCs,
                "fr" => post.ContentFr ?? post.ContentCs,
                "hi" => post.ContentHi ?? post.ContentCs,
                "ja" => post.ContentJa ?? post.ContentCs,
                "pt" => post.ContentPt ?? post.ContentCs,
                "ru" => post.ContentRu ?? post.ContentCs,
                "zh" => post.ContentZh ?? post.ContentCs,
                _ => post.ContentCs
            };
        }
    }
}
