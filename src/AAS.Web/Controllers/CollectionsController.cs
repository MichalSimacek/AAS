using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AAS.Web.Controllers
{
    public class CollectionsController : Controller
    {
        private readonly AppDbContext _db; private readonly TranslationService _tr;
        public CollectionsController(AppDbContext db, TranslationService tr) { _db = db; _tr = tr; }

        public async Task<IActionResult> Index(CollectionCategory? category, int page = 1)
        {
            const int pageSize = 12;

            // PERFORMANCE FIX: Only load first image for thumbnail, not all images
            var q = _db.Collections
                .Select(c => new
                {
                    Collection = c,
                    FirstImage = c.Images.OrderBy(i => i.SortOrder).FirstOrDefault()
                })
                .OrderByDescending(x => x.Collection.CreatedUtc)
                .AsQueryable();

            if (category.HasValue)
                q = q.Where(x => x.Collection.Category == category);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // Manually attach first image to collection for view
            foreach (var item in items)
            {
                if (item.FirstImage != null)
                {
                    item.Collection.Images = new List<CollectionImage> { item.FirstImage };
                }
            }

            // Translate titles if not English
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var translations = new Dictionary<int, string>();
            
            if (lang != "en")
            {
                foreach (var item in items)
                {
                    var translatedTitle = await _tr.TranslateAsync(item.Collection.Title, "en", lang);
                    translations[item.Collection.Id] = translatedTitle;
                }
            }

            ViewBag.Translations = translations;
            ViewBag.Category = category;
            return View(items.Select(x => x.Collection).ToList());
        }

        [Route("collections/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            // PERFORMANCE: Use AsNoTracking for read-only operations
            var item = await _db.Collections
                .Include(c => c.Images.OrderBy(i => i.SortOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug);

            if (item == null) return NotFound();

            // Translate title and description on the fly based on UI culture
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (lang != "en")
            {
                ViewBag.TranslatedTitle = await _tr.TranslateAsync(item.Title, "en", lang);
                ViewBag.TranslatedDescription = await _tr.TranslateAsync(item.Description, "en", lang);
            }
            else
            {
                ViewBag.TranslatedTitle = item.Title;
                ViewBag.TranslatedDescription = item.Description;
            }

            return View("Detail", item);
        }
    }
}