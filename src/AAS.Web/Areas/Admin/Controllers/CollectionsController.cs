using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CollectionsController : Controller
    {
        private readonly AppDbContext _db; private readonly SlugService _slug; private readonly ImageService _img; private readonly TranslationService _tr; private readonly IConfiguration _cfg;
        public CollectionsController(AppDbContext db, SlugService slug, ImageService img, TranslationService tr, IConfiguration cfg) { _db = db; _slug = slug; _img = img; _tr = tr; _cfg = cfg; }

        public async Task<IActionResult> Index()
        {
            // PERFORMANCE FIX: Don't load images for list view, only count
            var items = await _db.Collections
                .Select(c => new
                {
                    Collection = c,
                    ImageCount = c.Images.Count()
                })
                .OrderByDescending(x => x.Collection.CreatedUtc)
                .AsNoTracking()
                .ToListAsync();

            // Pass image counts via ViewBag
            ViewBag.ImageCounts = items.ToDictionary(x => x.Collection.Id, x => x.ImageCount);
            return View(items.Select(x => x.Collection).ToList());
        }

        public IActionResult Create() => View(new Collection());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB max
        public async Task<IActionResult> Create(Collection model, List<IFormFile> images, IFormFile? audio)
        {
            if (!ModelState.IsValid) return View(model);
            
            // Generate unique slug
            var baseSlug = _slug.ToSlug(model.Title);
            var slug = baseSlug;
            var counter = 1;
            
            while (await _db.Collections.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            
            model.Slug = slug;

            // Security: Validate at least one image is provided
            if (!images.Any(f => f.Length > 0))
            {
                ModelState.AddModelError("images", "At least one image is required");
                return View(model);
            }

            // PERFORMANCE FIX: Process audio file BEFORE transaction (I/O should not be in transaction)
            string? savedAudioPath = null;
            if (audio != null && audio.Length > 0)
            {
                var allowedAudioExt = new[] { ".mp3" };
                var audioExt = Path.GetExtension(audio.FileName).ToLowerInvariant();
                if (!allowedAudioExt.Contains(audioExt))
                {
                    ModelState.AddModelError("audio", $"Only MP3 files are allowed");
                    return View(model);
                }

                const int maxAudioSizeMB = 15;
                if (audio.Length > maxAudioSizeMB * 1024 * 1024)
                {
                    ModelState.AddModelError("audio", $"Audio file size must be less than {maxAudioSizeMB}MB");
                    return View(model);
                }

                try
                {
                    var audioDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/audio");
                    Directory.CreateDirectory(audioDir);
                    var audioName = Guid.NewGuid().ToString("N") + audioExt;
                    var audioPath = Path.Combine(audioDir, audioName);

                    await using (var fs = new FileStream(audioPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                    {
                        await audio.CopyToAsync(fs);
                    }

                    savedAudioPath = "/uploads/audio/" + audioName;
                    model.AudioPath = savedAudioPath;
                }
                catch (Exception audioEx)
                {
                    ModelState.AddModelError("audio", $"Error saving audio: {audioEx.Message}");
                    return View(model);
                }
            }

            // PERFORMANCE FIX: Process all images BEFORE transaction (I/O should not be in transaction)
            var savedImages = new List<(string fileName, int width, int height, long bytes)>();
            var imageErrors = new List<string>();

            foreach (var f in images.Where(f => f.Length > 0))
            {
                try
                {
                    Console.WriteLine($"Processing image: {f.FileName} ({f.Length} bytes)");
                    var nameNoExt = Guid.NewGuid().ToString("N");
                    var meta = await _img.SaveOriginalAndVariantsAsync(f, nameNoExt);
                    savedImages.Add((nameNoExt, meta.w, meta.h, meta.b));
                    Console.WriteLine($"Image {f.FileName} processed successfully");
                }
                catch (Exception imgEx)
                {
                    imageErrors.Add($"{f.FileName}: {imgEx.Message}");
                    Console.WriteLine($"Image {f.FileName} failed: {imgEx.Message}");
                }
            }

            if (savedImages.Count == 0)
            {
                // Clean up audio if all images failed
                if (savedAudioPath != null)
                {
                    try
                    {
                        var audioFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + savedAudioPath.TrimStart('/'));
                        if (System.IO.File.Exists(audioFullPath))
                            System.IO.File.Delete(audioFullPath);
                    }
                    catch { }
                }

                ModelState.AddModelError("images", $"All images failed. Errors: {string.Join("; ", imageErrors)}");
                return View(model);
            }

            // NOW start transaction (fast DB operations only)
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    _db.Collections.Add(model);
                    await _db.SaveChangesAsync();

                    // Add all successfully processed images to DB
                    int order = 0;
                    foreach (var (fileName, width, height, bytes) in savedImages)
                    {
                        var imgEntity = new CollectionImage
                        {
                            CollectionId = model.Id,
                            FileName = fileName,
                            Width = width,
                            Height = height,
                            Bytes = bytes,
                            SortOrder = order++
                        };
                        _db.CollectionImages.Add(imgEntity);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // PERFORMANCE FIX: Translate AFTER transaction commits (HTTP calls should not be in transaction)
                    try
                    {
                        await TranslateCollectionAsync(model);
                    }
                    catch (Exception transEx)
                    {
                        Console.WriteLine($"Translation failed (non-fatal): {transEx.Message}");
                    }

                    var successMsg = $"Collection '{model.Title}' created with {savedImages.Count} image(s)";
                    if (imageErrors.Count > 0)
                    {
                        successMsg += $". {imageErrors.Count} image(s) failed: {string.Join(", ", imageErrors)}";
                    }
                    TempData["SuccessMessage"] = successMsg;

                    Console.WriteLine($"Collection created: {savedImages.Count} success, {imageErrors.Count} failed");
                    return RedirectToAction(nameof(Index), new { area = "Admin" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Transaction failed: {ex.Message}");

                    // Clean up saved files since DB transaction failed
                    foreach (var (fileName, _, _, _) in savedImages)
                    {
                        try { _img.DeleteAllVariants(fileName); } catch { }
                    }

                    if (savedAudioPath != null)
                    {
                        try
                        {
                            var audioFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + savedAudioPath.TrimStart('/'));
                            if (System.IO.File.Exists(audioFullPath))
                                System.IO.File.Delete(audioFullPath);
                        }
                        catch { }
                    }

                    ModelState.AddModelError("", $"Error creating collection: {ex.Message}");
                    return View(model);
                }
            });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Collections.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();

            Console.WriteLine($"[EDIT GET DEBUG] Collection ID: {id}");
            Console.WriteLine($"[EDIT GET DEBUG] Description from DB: {item.Description?.Substring(0, Math.Min(100, item.Description?.Length ?? 0))}...");
            Console.WriteLine($"[EDIT GET DEBUG] Description Length: {item.Description?.Length ?? 0}");

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB max
        public async Task<IActionResult> Edit(int id, Collection model, List<IFormFile> newImages)
        {
            // Validate Title for ModelState
            if (string.IsNullOrEmpty(model.Title))
            {
                ModelState.Remove("Title");
            }

            if (!ModelState.IsValid)
            {
                var temp = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id);
                TempData["ErrorMessage"] = "Please fix validation errors.";
                return View(temp ?? new Collection());
            }

            // PERFORMANCE FIX: Process new images BEFORE transaction (I/O should not be in transaction)
            var savedNewImages = new List<(string fileName, int width, int height, long bytes)>();
            if (newImages != null && newImages.Any(f => f.Length > 0))
            {
                foreach (var f in newImages.Where(f => f.Length > 0))
                {
                    try
                    {
                        var nameNoExt = Guid.NewGuid().ToString("N");
                        var meta = await _img.SaveOriginalAndVariantsAsync(f, nameNoExt);
                        savedNewImages.Add((nameNoExt, meta.w, meta.h, meta.b));
                    }
                    catch (Exception imgEx)
                    {
                        Console.WriteLine($"Image {f.FileName} failed: {imgEx.Message}");
                        // Continue with other images
                    }
                }
            }

            // CRITICAL FIX: Use ExecutionStrategy for retrying transactions
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    // CRITICAL: Load entity INSIDE ExecutionStrategy to ensure proper tracking
                    var existing = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id);
                    if (existing == null) return NotFound();

                    // Update properties - handle empty strings as "keep existing value"
                    existing.Title = !string.IsNullOrWhiteSpace(model.Title) ? model.Title : existing.Title;
                    existing.Description = !string.IsNullOrWhiteSpace(model.Description) ? model.Description : existing.Description;
                    existing.Category = model.Category;
                    existing.Slug = _slug.ToSlug(existing.Title);

                    // CRITICAL: Mark entity as modified to ensure EF Core tracks changes
                    _db.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                    // Add new images that were successfully processed
                    if (savedNewImages.Any())
                    {
                        var maxOrder = await _db.CollectionImages
                            .Where(i => i.CollectionId == existing.Id)
                            .Select(i => (int?)i.SortOrder)
                            .MaxAsync() ?? -1;

                        int order = maxOrder + 1;
                        foreach (var (fileName, width, height, bytes) in savedNewImages)
                        {
                            _db.CollectionImages.Add(new CollectionImage
                            {
                                CollectionId = existing.Id,
                                FileName = fileName,
                                Width = width,
                                Height = height,
                                Bytes = bytes,
                                SortOrder = order++
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // PERFORMANCE FIX: Translate AFTER transaction commits (HTTP calls should not be in transaction)
                    try
                    {
                        await TranslateCollectionAsync(existing);
                    }
                    catch (Exception transEx)
                    {
                        Console.WriteLine($"Translation failed (non-fatal): {transEx.Message}");
                    }

                    TempData["SuccessMessage"] = $"Collection '{existing.Title}' updated successfully.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error updating collection: {ex.Message}");

                    // Clean up saved images since DB transaction failed
                    foreach (var (fileName, _, _, _) in savedNewImages)
                    {
                        try { _img.DeleteAllVariants(fileName); } catch { }
                    }

                    TempData["ErrorMessage"] = $"Error updating collection: {ex.Message}";
                    return RedirectToAction(nameof(Edit), new { id });
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var collection = await _db.Collections
                    .Include(c => c.Images)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (collection == null)
                {
                    TempData["ErrorMessage"] = "Collection not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Delete image files
                foreach (var img in collection.Images)
                {
                    try
                    {
                        _img.DeleteAllVariants(img.FileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete image {img.FileName}: {ex.Message}");
                        // Continue deleting other images even if one fails
                    }
                }

                // Delete audio file if exists
                if (!string.IsNullOrEmpty(collection.AudioPath))
                {
                    try
                    {
                        var audioPath = Path.Combine("wwwroot", collection.AudioPath.TrimStart('/'));
                        if (System.IO.File.Exists(audioPath))
                        {
                            System.IO.File.Delete(audioPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete audio file: {ex.Message}");
                    }
                }

                // Delete from database
                _db.Collections.Remove(collection);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Collection '{collection.Title}' deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting collection: {ex.Message}");
                TempData["ErrorMessage"] = $"Error deleting collection: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("Admin/Collections/DeleteImage/{id}/{imageId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            try
            {
                var collection = await _db.Collections
                    .Include(c => c.Images)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (collection == null)
                {
                    TempData["ErrorMessage"] = "Collection not found.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                var image = collection.Images.FirstOrDefault(i => i.Id == imageId);
                if (image == null)
                {
                    TempData["ErrorMessage"] = "Image not found.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                // Delete image files from disk
                try
                {
                    _img.DeleteAllVariants(image.FileName);
                    Console.WriteLine($"Deleted image files for: {image.FileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete image files: {ex.Message}");
                    // Continue to remove from database even if file deletion fails
                }

                // Remove from database
                _db.CollectionImages.Remove(image);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Image deleted successfully.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
                TempData["ErrorMessage"] = $"Error deleting image: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [HttpPost]
        [Route("Admin/Collections/ReorderImages/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderImages(int id, [FromBody] List<int> imageIds)
        {
            try
            {
                Console.WriteLine($"[ReorderImages] START - Collection {id}, Received IDs: [{string.Join(", ", imageIds ?? new List<int>())}]");

                if (imageIds == null || imageIds.Count == 0)
                {
                    Console.WriteLine("[ReorderImages] ERROR: No image IDs provided");
                    return Json(new { success = false, message = "No image IDs provided" });
                }

                // Load images - tracked entities
                var images = await _db.CollectionImages
                    .Where(img => img.CollectionId == id)
                    .OrderBy(img => img.SortOrder)
                    .ToListAsync();

                Console.WriteLine($"[ReorderImages] Loaded {images.Count} images from DB");
                foreach (var img in images)
                {
                    Console.WriteLine($"[ReorderImages] DB Image {img.Id}: current SortOrder = {img.SortOrder}");
                }

                if (images.Count == 0)
                {
                    Console.WriteLine($"[ReorderImages] ERROR: No images found for collection {id}");
                    return Json(new { success = false, message = "Collection not found or has no images" });
                }

                // Update sort order for each image in the new order
                int changedCount = 0;
                for (int i = 0; i < imageIds.Count; i++)
                {
                    var imageId = imageIds[i];
                    var image = images.FirstOrDefault(img => img.Id == imageId);
                    if (image != null)
                    {
                        if (image.SortOrder != i)
                        {
                            var oldOrder = image.SortOrder;
                            image.SortOrder = i;
                            _db.Entry(image).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            changedCount++;
                            Console.WriteLine($"[ReorderImages] CHANGED Image {imageId}: {oldOrder} -> {i}");
                        }
                        else
                        {
                            Console.WriteLine($"[ReorderImages] UNCHANGED Image {imageId}: already at position {i}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ReorderImages] WARNING: Image {imageId} not found in collection {id}");
                    }
                }

                Console.WriteLine($"[ReorderImages] About to save {changedCount} changes to database...");
                var savedChanges = await _db.SaveChangesAsync();
                Console.WriteLine($"[ReorderImages] SUCCESS: SaveChangesAsync returned {savedChanges}");

                // Verify changes were saved
                var verifyImages = await _db.CollectionImages
                    .Where(img => img.CollectionId == id)
                    .OrderBy(img => img.SortOrder)
                    .AsNoTracking()
                    .ToListAsync();

                Console.WriteLine($"[ReorderImages] VERIFICATION - After save:");
                foreach (var img in verifyImages)
                {
                    Console.WriteLine($"[ReorderImages] VERIFY Image {img.Id}: SortOrder = {img.SortOrder}");
                }

                return Json(new { success = true, message = $"Order updated successfully ({savedChanges} changes)" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReorderImages] ERROR: {ex.Message}");
                Console.WriteLine($"[ReorderImages] Stack: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Diagnostic page showing translation status
        /// </summary>
        public async Task<IActionResult> TranslationStatus()
        {
            var collections = await _db.Collections.ToListAsync();
            var translations = await _db.CollectionTranslations.ToListAsync();

            var status = new System.Text.StringBuilder();
            status.AppendLine($"<h2>Translation Status</h2>");
            status.AppendLine($"<p><strong>Total Collections:</strong> {collections.Count}</p>");
            status.AppendLine($"<p><strong>Total Translations:</strong> {translations.Count}</p>");
            status.AppendLine($"<p><strong>Translation Service Enabled:</strong> {_cfg["Translation:Enabled"]}</p>");
            status.AppendLine($"<p><strong>Translation Endpoint:</strong> {_cfg["Translation:Endpoint"]}</p>");
            status.AppendLine($"<hr>");

            foreach (var col in collections)
            {
                var colTranslations = translations.Where(t => t.CollectionId == col.Id).ToList();
                status.AppendLine($"<h3>Collection #{col.Id}: {col.Title}</h3>");
                status.AppendLine($"<p>Translations: {colTranslations.Count}</p>");
                if (colTranslations.Any())
                {
                    status.AppendLine("<ul>");
                    foreach (var t in colTranslations)
                    {
                        status.AppendLine($"<li><strong>{t.LanguageCode}:</strong> {t.TranslatedTitle}</li>");
                    }
                    status.AppendLine("</ul>");
                }
                else
                {
                    status.AppendLine("<p style='color: red;'>⚠️ No translations found!</p>");
                }
            }

            return Content(status.ToString(), "text/html");
        }

        /// <summary>
        /// Create dummy/mock translations for testing (without calling translation API)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDummyTranslations()
        {
            try
            {
                var collections = await _db.Collections.ToListAsync();
                var supportedCultures = _cfg.GetSection("Localization:SupportedCultures").Get<string[]>() ?? new[] { "en" };

                // Remove existing translations
                var existingTranslations = await _db.CollectionTranslations.ToListAsync();
                _db.CollectionTranslations.RemoveRange(existingTranslations);

                var langNames = new Dictionary<string, string>
                {
                    ["cs"] = "(Czech)",
                    ["ru"] = "(Russian)",
                    ["de"] = "(German)",
                    ["es"] = "(Spanish)",
                    ["fr"] = "(French)",
                    ["zh"] = "(Chinese)",
                    ["pt"] = "(Portuguese)",
                    ["hi"] = "(Hindi)",
                    ["ja"] = "(Japanese)"
                };

                foreach (var collection in collections)
                {
                    foreach (var lang in supportedCultures.Where(l => l != "en"))
                    {
                        var translation = new CollectionTranslation
                        {
                            CollectionId = collection.Id,
                            LanguageCode = lang,
                            TranslatedTitle = $"{collection.Title} {langNames.GetValueOrDefault(lang, $"({lang})")}",
                            TranslatedDescription = string.IsNullOrWhiteSpace(collection.Description)
                                ? $"[{langNames.GetValueOrDefault(lang, lang)} translation]"
                                : $"{collection.Description} {langNames.GetValueOrDefault(lang, $"({lang})")}",
                            CreatedUtc = DateTime.UtcNow
                        };
                        _db.CollectionTranslations.Add(translation);
                    }
                }

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Created dummy translations for {collections.Count} collections for testing purposes.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Delete all translations (for cleanup)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllTranslations()
        {
            try
            {
                var translations = await _db.CollectionTranslations.ToListAsync();
                _db.CollectionTranslations.RemoveRange(translations);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Deleted {translations.Count} translations. You can now retranslate.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Admin action to translate all existing collections (for collections created before auto-translation was implemented)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranslateAllCollections()
        {
            try
            {
                Console.WriteLine("[TranslateAll] Starting translation of all collections...");

                var collections = await _db.Collections.ToListAsync();
                Console.WriteLine($"[TranslateAll] Found {collections.Count} collections to translate");

                int successCount = 0;
                int failCount = 0;

                foreach (var collection in collections)
                {
                    try
                    {
                        Console.WriteLine($"[TranslateAll] Translating collection {collection.Id}: {collection.Title}");
                        await TranslateCollectionAsync(collection);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TranslateAll] Failed to translate collection {collection.Id}: {ex.Message}");
                        failCount++;
                    }
                }

                Console.WriteLine($"[TranslateAll] Complete! Success: {successCount}, Failed: {failCount}");
                TempData["SuccessMessage"] = $"Translation completed! {successCount} collections translated successfully" + (failCount > 0 ? $", {failCount} failed." : ".");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TranslateAll] ERROR: {ex.Message}");
                TempData["ErrorMessage"] = $"Error translating collections: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Automatically translates collection title and description to all supported languages
        /// </summary>
        private async Task TranslateCollectionAsync(Collection collection)
        {
            var supportedCultures = _cfg.GetSection("Localization:SupportedCultures").Get<string[]>() ?? new[] { "en" };

            // Remove existing translations for this collection
            var existingTranslations = await _db.CollectionTranslations
                .Where(t => t.CollectionId == collection.Id)
                .ToListAsync();
            _db.CollectionTranslations.RemoveRange(existingTranslations);

            // Translate to all supported languages (except English, which is the source)
            foreach (var lang in supportedCultures.Where(l => l != "en"))
            {
                try
                {
                    var translatedTitle = await _tr.TranslateAsync(collection.Title, "en", lang);
                    var translatedDescription = await _tr.TranslateAsync(collection.Description, "en", lang);

                    var translation = new CollectionTranslation
                    {
                        CollectionId = collection.Id,
                        LanguageCode = lang,
                        TranslatedTitle = translatedTitle,
                        TranslatedDescription = translatedDescription,
                        CreatedUtc = DateTime.UtcNow
                    };

                    _db.CollectionTranslations.Add(translation);
                    Console.WriteLine($"Translated collection '{collection.Title}' to {lang}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to translate collection to {lang}: {ex.Message}");
                    // Continue with other languages even if one fails
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}