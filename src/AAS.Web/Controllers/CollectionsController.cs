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
                var collectionIds = collections.Select(c => c.Id).ToList();
                var dbTranslations = await _db.CollectionTranslations
                    .Where(t => collectionIds.Contains(t.CollectionId) && t.LanguageCode == lang)
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.CollectionId, t => t.TranslatedTitle);

                foreach (var collection in collections)
                {
                    if (dbTranslations.TryGetValue(collection.Id, out var translatedTitle))
                    {
                        translations[collection.Id] = translatedTitle;
                    }
                    else
                    {
                        // Fallback to on-demand translation if not found in database
                        translations[collection.Id] = await _tr.TranslateAsync(collection.Title, "en", lang);
                    }
                }
            }

            ViewBag.Translations = translations;
            ViewBag.Category = category;
            return View(collections);
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
            
            // Czech is the source language, no translation needed
            if (lang == "cs")
            {
                ViewBag.TranslatedTitle = item.Title;
                ViewBag.TranslatedDescription = item.Description;
            }
            else
            {
                // Try to load translation from database
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
                    ViewBag.TranslatedTitle = await _tr.TranslateAsync(item.Title, "cs", lang);
                    ViewBag.TranslatedDescription = await _tr.TranslateAsync(item.Description, "cs", lang);
                }
            }

            return View("Detail", item);
        }
    }
}