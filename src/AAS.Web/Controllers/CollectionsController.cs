using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AAS.Web.Controllers
{
    public class CollectionsController : Controller
    {
        private readonly AppDbContext _db; private readonly TranslationService _tr;
        public CollectionsController(AppDbContext db, TranslationService tr) { _db = db; _tr = tr; }

        // Helper to extract numeric price from string (e.g., "15,000" -> 15000, "Price on request" -> 0)
        private static decimal ParsePrice(string? price)
        {
            if (string.IsNullOrWhiteSpace(price)) return 0;
            // Remove all non-numeric characters except decimal point and comma
            var numericString = Regex.Replace(price, @"[^\d.,]", "");
            // Replace comma with dot for parsing
            numericString = numericString.Replace(",", "");
            if (decimal.TryParse(numericString, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            return 0;
        }

        public async Task<IActionResult> Index(CollectionCategory? category, string? sort, string? status, int page = 1)
        {
            const int pageSize = 12;

            // Load collections with first image
            var q = _db.Collections
                .Include(c => c.Images.OrderBy(i => i.SortOrder).Take(1))
                .AsQueryable();

            if (category.HasValue)
                q = q.Where(c => c.Category == category);

            // Filter by status if specified
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CollectionStatus>(status, out var statusFilter))
                q = q.Where(c => c.Status == statusFilter);

            // Get total count for pagination
            var totalCount = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Load all matching collections for sorting (we need to sort by parsed price in memory)
            var allCollections = await q.AsNoTracking().ToListAsync();

            // Apply sorting - default: Available first, then by price descending
            IEnumerable<Collection> sortedCollections = sort switch
            {
                "price_asc" => allCollections
                    .OrderBy(c => c.Status)
                    .ThenBy(c => ParsePrice(c.Price)),
                "price_desc" => allCollections
                    .OrderBy(c => c.Status)
                    .ThenByDescending(c => ParsePrice(c.Price)),
                "status_available" => allCollections
                    .Where(c => c.Status == CollectionStatus.Available)
                    .OrderByDescending(c => ParsePrice(c.Price))
                    .Concat(allCollections.Where(c => c.Status != CollectionStatus.Available)
                        .OrderBy(c => c.Status).ThenByDescending(c => ParsePrice(c.Price))),
                "status_sold" => allCollections
                    .Where(c => c.Status == CollectionStatus.Sold)
                    .OrderByDescending(c => ParsePrice(c.Price))
                    .Concat(allCollections.Where(c => c.Status != CollectionStatus.Sold)
                        .OrderBy(c => c.Status).ThenByDescending(c => ParsePrice(c.Price))),
                "status_auction" => allCollections
                    .Where(c => c.Status == CollectionStatus.InAuction)
                    .OrderByDescending(c => ParsePrice(c.Price))
                    .Concat(allCollections.Where(c => c.Status != CollectionStatus.InAuction)
                        .OrderBy(c => c.Status).ThenByDescending(c => ParsePrice(c.Price))),
                _ => allCollections // Default: Available first, price descending
                    .OrderBy(c => c.Status)
                    .ThenByDescending(c => ParsePrice(c.Price))
            };

            // Apply pagination
            var collections = sortedCollections
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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