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

            // Load collections with first image
            var q = _db.Collections
                .Include(c => c.Images.OrderBy(i => i.SortOrder).Take(1))
                .OrderByDescending(c => c.CreatedUtc)
                .AsQueryable();

            if (category.HasValue)
                q = q.Where(c => c.Category == category);

            var collections = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // Load pre-translated titles from database
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var translations = new Dictionary<int, string>();

            if (lang != "en")
            {
                var collectionIds = items.Select(x => x.Collection.Id).ToList();
                var dbTranslations = await _db.CollectionTranslations
                    .Where(t => collectionIds.Contains(t.CollectionId) && t.LanguageCode == lang)
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.CollectionId, t => t.TranslatedTitle);

                foreach (var item in items)
                {
                    if (dbTranslations.TryGetValue(item.Collection.Id, out var translatedTitle))
                    {
                        translations[item.Collection.Id] = translatedTitle;
                    }
                    else
                    {
                        // Fallback to on-demand translation if not found in database
                        translations[item.Collection.Id] = await _tr.TranslateAsync(item.Collection.Title, "en", lang);
                    }
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

            // Load pre-translated content from database
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (lang != "en")
            {
                var translation = await _db.CollectionTranslations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.CollectionId == item.Id && t.LanguageCode == lang);

                if (translation != null)
                {
                    ViewBag.TranslatedTitle = translation.TranslatedTitle;
                    ViewBag.TranslatedDescription = translation.TranslatedDescription;
                }
                else
                {
                    // Fallback to on-demand translation if not found in database
                    ViewBag.TranslatedTitle = await _tr.TranslateAsync(item.Title, "en", lang);
                    ViewBag.TranslatedDescription = await _tr.TranslateAsync(item.Description, "en", lang);
                }
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