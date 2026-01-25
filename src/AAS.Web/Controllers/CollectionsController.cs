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
                .AsQueryable();

            if (category.HasValue)
                q = q.Where(c => c.Category == category);

            // Get total count for pagination
            var totalCount = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Order by status: Available (0) → InAuction (1) → Sold (2), then by CreatedUtc descending
            var collections = await q
                .OrderBy(c => c.Status)
                .ThenByDescending(c => c.CreatedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // Load pre-translated titles from database
            // Original collection titles are in Czech (cs), so we need translations for ALL other languages
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var translations = new Dictionary<int, string>();
            
            // Only load translations if current language is NOT Czech (the original language)
            if (lang != "cs")
            {
                var collectionIds = collections.Select(c => c.Id).ToList();
                
                // Load translations for requested language
                var dbTranslations = await _db.CollectionTranslations
                    .Where(t => collectionIds.Contains(t.CollectionId) && t.LanguageCode == lang)
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.CollectionId, t => t.TranslatedTitle);
                
                // Also load English translations as fallback
                var englishTranslations = lang != "en" 
                    ? await _db.CollectionTranslations
                        .Where(t => collectionIds.Contains(t.CollectionId) && t.LanguageCode == "en")
                        .AsNoTracking()
                        .ToDictionaryAsync(t => t.CollectionId, t => t.TranslatedTitle)
                    : dbTranslations;

                foreach (var collection in collections)
                {
                    if (dbTranslations.TryGetValue(collection.Id, out var translatedTitle))
                    {
                        // Use translation in requested language
                        translations[collection.Id] = translatedTitle;
                    }
                    else
                    {
                        // Try on-demand translation first
                        var onDemandTranslation = await _tr.TranslateAsync(collection.Title, "cs", lang);
                        
                        // If on-demand translation failed (returned original Czech text), use English fallback
                        if (onDemandTranslation == collection.Title && englishTranslations.TryGetValue(collection.Id, out var englishTitle))
                        {
                            translations[collection.Id] = englishTitle;
                        }
                        else
                        {
                            translations[collection.Id] = onDemandTranslation;
                        }
                    }
                }
            }

            ViewBag.Translations = translations;
            ViewBag.Category = category;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            return View(collections);
        }

        public async Task<IActionResult> Details(string id)
        {
            // PERFORMANCE: Use AsNoTracking for read-only operations
            var item = await _db.Collections
                .Include(c => c.Images.OrderBy(i => i.SortOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == id);

            if (item == null) return NotFound();

            // Load pre-translated content from database
            // Original content is in Czech (cs), so only translate for other languages
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            
            if (lang != "cs")
            {
                // Try to load translation from database for requested language
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
                    // Try on-demand translation first
                    var onDemandTitle = await _tr.TranslateAsync(item.Title, "cs", lang);
                    var onDemandDescription = await _tr.TranslateAsync(item.Description, "cs", lang);
                    
                    // If on-demand translation failed (returned original Czech text), try English fallback
                    if (onDemandTitle == item.Title && lang != "en")
                    {
                        var englishTranslation = await _db.CollectionTranslations
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.CollectionId == item.Id && t.LanguageCode == "en");
                        
                        if (englishTranslation != null)
                        {
                            ViewBag.TranslatedTitle = englishTranslation.TranslatedTitle;
                            ViewBag.TranslatedDescription = englishTranslation.TranslatedDescription;
                        }
                        else
                        {
                            ViewBag.TranslatedTitle = onDemandTitle;
                            ViewBag.TranslatedDescription = onDemandDescription;
                        }
                    }
                    else
                    {
                        ViewBag.TranslatedTitle = onDemandTitle;
                        ViewBag.TranslatedDescription = onDemandDescription;
                    }
                }
            }
            else
            {
                // Czech language - use original content (no translation needed)
                ViewBag.TranslatedTitle = item.Title;
                ViewBag.TranslatedDescription = item.Description;
            }

            return View("Detail", item);
        }
    }
}