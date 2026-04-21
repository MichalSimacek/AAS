using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using System.Security.Claims;

namespace AAS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BlogController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IDeepLService _deepL;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHtmlSanitizer _sanitizer;
        private readonly ILogger<BlogController> _logger;

        // SECURITY: File upload constraints
        private static readonly string[] AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxImageBytes = 8L * 1024 * 1024; // 8 MB

        public BlogController(
            AppDbContext db,
            IDeepLService deepL,
            UserManager<IdentityUser> userManager,
            IHtmlSanitizer sanitizer,
            ILogger<BlogController> logger)
        {
            _db = db;
            _deepL = deepL;
            _userManager = userManager;
            _sanitizer = sanitizer;
            _logger = logger;
        }

        // SECURITY: Validate and save an uploaded image safely.
        // - Enforces extension whitelist
        // - Enforces max size
        // - Verifies it is a real image by decoding with ImageSharp
        // - Uses GUID-only filename (never trusts client filename -> prevents path traversal / dangerous extensions)
        // Returns public URL or null if invalid.
        private async Task<string?> SaveFeaturedImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaxImageBytes) throw new InvalidOperationException("Image too large (max 8 MB).");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                throw new InvalidOperationException("Unsupported image type.");

            // Verify file is actually a valid image
            try
            {
                await using var probe = file.OpenReadStream();
                using var img = await Image.LoadAsync(probe);
                if (img.Width < 1 || img.Height < 1)
                    throw new InvalidOperationException("Invalid image.");
            }
            catch (InvalidOperationException) { throw; }
            catch
            {
                throw new InvalidOperationException("File is not a valid image.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "blog");
            Directory.CreateDirectory(uploadsFolder);

            // SECURITY: Use GUID only; never trust user-supplied filename
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, safeName);

            await using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return $"/uploads/blog/{safeName}";
        }

        // GET: Admin/Blog
        public async Task<IActionResult> Index()
        {
            var posts = await _db.BlogPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET: Admin/Blog/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost post, IFormFile? featuredImage)
        {
            // DIAGNOSTIC LOGGING
            _logger.LogInformation("===== BLOG POST CREATE ATTEMPT =====");
            _logger.LogInformation($"TitleCs: {post.TitleCs}");
            _logger.LogInformation($"ContentCs: {post.ContentCs ?? "NULL"}");
            _logger.LogInformation($"ContentCs Length: {post.ContentCs?.Length ?? 0}");
            _logger.LogInformation($"Published: {post.Published}");
            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is INVALID. Errors:");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"  - {error.ErrorMessage}");
                    }
                }
                return View(post);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                post.AuthorId = userId;
                post.CreatedAt = DateTime.UtcNow;

                // SECURITY: Sanitize the WYSIWYG HTML input to prevent stored XSS
                if (!string.IsNullOrEmpty(post.ContentCs))
                    post.ContentCs = _sanitizer.Sanitize(post.ContentCs);

                // Handle featured image upload (hardened: extension + size + image-content validation + GUID filename)
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    try
                    {
                        var url = await SaveFeaturedImageAsync(featuredImage);
                        if (url != null) post.FeaturedImage = url;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError(nameof(featuredImage), ex.Message);
                        return View(post);
                    }
                }

                // Translate title and content to all languages using DeepL
                // Uses automatic language detection - admin can write in any language
                try
                {
                    _logger.LogInformation("Starting DeepL translations for blog post...");

                    // Translate ONE BY ONE like Collections do (avoids parallel 413 errors)
                    _logger.LogInformation("Starting sequential translations (title + content per language)");
                    
                    var targetLanguages = new[] { "en", "de", "es", "fr", "hi", "ja", "pt", "ru", "zh" };
                    
                    foreach (var lang in targetLanguages)
                    {
                        try
                        {
                            _logger.LogInformation($"Translating to {lang}...");
                            
                            // Translate title
                            var translatedTitle = await _deepL.TranslateAsync(post.TitleCs, lang, "cs");
                            
                            // Translate content
                            var translatedContent = await _deepL.TranslateAsync(post.ContentCs, lang, "cs");
                            
                            // Assign to corresponding properties
                            switch (lang)
                            {
                                case "en":
                                    post.TitleEn = translatedTitle;
                                    post.ContentEn = translatedContent;
                                    break;
                                case "de":
                                    post.TitleDe = translatedTitle;
                                    post.ContentDe = translatedContent;
                                    break;
                                case "es":
                                    post.TitleEs = translatedTitle;
                                    post.ContentEs = translatedContent;
                                    break;
                                case "fr":
                                    post.TitleFr = translatedTitle;
                                    post.ContentFr = translatedContent;
                                    break;
                                case "hi":
                                    post.TitleHi = translatedTitle;
                                    post.ContentHi = translatedContent;
                                    break;
                                case "ja":
                                    post.TitleJa = translatedTitle;
                                    post.ContentJa = translatedContent;
                                    break;
                                case "pt":
                                    post.TitlePt = translatedTitle;
                                    post.ContentPt = translatedContent;
                                    break;
                                case "ru":
                                    post.TitleRu = translatedTitle;
                                    post.ContentRu = translatedContent;
                                    break;
                                case "zh":
                                    post.TitleZh = translatedTitle;
                                    post.ContentZh = translatedContent;
                                    break;
                            }
                            
                            _logger.LogInformation($"✓ {lang} translation completed");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Translation failed for {lang}, using Czech as fallback");
                            // Fallback to Czech if translation fails
                            switch (lang)
                            {
                                case "en": post.TitleEn = post.TitleCs; post.ContentEn = post.ContentCs; break;
                                case "de": post.TitleDe = post.TitleCs; post.ContentDe = post.ContentCs; break;
                                case "es": post.TitleEs = post.TitleCs; post.ContentEs = post.ContentCs; break;
                                case "fr": post.TitleFr = post.TitleCs; post.ContentFr = post.ContentCs; break;
                                case "hi": post.TitleHi = post.TitleCs; post.ContentHi = post.ContentCs; break;
                                case "ja": post.TitleJa = post.TitleCs; post.ContentJa = post.ContentCs; break;
                                case "pt": post.TitlePt = post.TitleCs; post.ContentPt = post.ContentCs; break;
                                case "ru": post.TitleRu = post.TitleCs; post.ContentRu = post.ContentCs; break;
                                case "zh": post.TitleZh = post.TitleCs; post.ContentZh = post.ContentCs; break;
                            }
                        }
                    }
                    
                    _logger.LogInformation("All translations completed");

                    _logger.LogInformation("DeepL translations completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeepL translation failed, using original Czech text for all languages");
                    // Fallback: use Czech for all languages
                    post.TitleEn = post.TitleDe = post.TitleEs = post.TitleFr = 
                                   post.TitleHi = post.TitleJa = post.TitlePt = 
                                   post.TitleRu = post.TitleZh = post.TitleCs;
                    post.ContentEn = post.ContentDe = post.ContentEs = post.ContentFr = 
                                     post.ContentHi = post.ContentJa = post.ContentPt = 
                                     post.ContentRu = post.ContentZh = post.ContentCs;
                }

                _db.BlogPosts.Add(post);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Blog post created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog post");
                ModelState.AddModelError("", "An error occurred while creating the blog post.");
                return View(post);
            }
        }

        // GET: Admin/Blog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Admin/Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost post, IFormFile? featuredImage)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(post);
            }

            try
            {
                var existingPost = await _db.BlogPosts.FindAsync(id);
                if (existingPost == null)
                {
                    return NotFound();
                }

                // Update basic fields (SECURITY: Sanitize WYSIWYG HTML)
                existingPost.TitleCs = post.TitleCs;
                existingPost.ContentCs = string.IsNullOrEmpty(post.ContentCs)
                    ? post.ContentCs
                    : _sanitizer.Sanitize(post.ContentCs);
                existingPost.Published = post.Published;
                existingPost.UpdatedAt = DateTime.UtcNow;

                // Handle new featured image (hardened)
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    try
                    {
                        var url = await SaveFeaturedImageAsync(featuredImage);
                        if (url != null) existingPost.FeaturedImage = url;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError(nameof(featuredImage), ex.Message);
                        return View(post);
                    }
                }

                // Re-translate if content changed (auto-detect source language)
                try
                {
                    _logger.LogInformation("Re-translating blog post {PostId}...", id);

                    // Translate ONE BY ONE like Collections do (avoids parallel 413 errors)
                    _logger.LogInformation("Starting sequential re-translations (title + content per language)");
                    
                    var targetLanguages = new[] { "en", "de", "es", "fr", "hi", "ja", "pt", "ru", "zh" };
                    
                    foreach (var lang in targetLanguages)
                    {
                        try
                        {
                            _logger.LogInformation($"Re-translating to {lang}...");
                            
                            // Translate title
                            var translatedTitle = await _deepL.TranslateAsync(existingPost.TitleCs, lang, "cs");
                            
                            // Translate content
                            var translatedContent = await _deepL.TranslateAsync(existingPost.ContentCs, lang, "cs");
                            
                            // Assign to corresponding properties
                            switch (lang)
                            {
                                case "en":
                                    existingPost.TitleEn = translatedTitle;
                                    existingPost.ContentEn = translatedContent;
                                    break;
                                case "de":
                                    existingPost.TitleDe = translatedTitle;
                                    existingPost.ContentDe = translatedContent;
                                    break;
                                case "es":
                                    existingPost.TitleEs = translatedTitle;
                                    existingPost.ContentEs = translatedContent;
                                    break;
                                case "fr":
                                    existingPost.TitleFr = translatedTitle;
                                    existingPost.ContentFr = translatedContent;
                                    break;
                                case "hi":
                                    existingPost.TitleHi = translatedTitle;
                                    existingPost.ContentHi = translatedContent;
                                    break;
                                case "ja":
                                    existingPost.TitleJa = translatedTitle;
                                    existingPost.ContentJa = translatedContent;
                                    break;
                                case "pt":
                                    existingPost.TitlePt = translatedTitle;
                                    existingPost.ContentPt = translatedContent;
                                    break;
                                case "ru":
                                    existingPost.TitleRu = translatedTitle;
                                    existingPost.ContentRu = translatedContent;
                                    break;
                                case "zh":
                                    existingPost.TitleZh = translatedTitle;
                                    existingPost.ContentZh = translatedContent;
                                    break;
                            }
                            
                            _logger.LogInformation($"✓ {lang} re-translation completed");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Re-translation failed for {lang}, using Czech as fallback");
                            // Fallback to Czech if translation fails
                            switch (lang)
                            {
                                case "en": existingPost.TitleEn = existingPost.TitleCs; existingPost.ContentEn = existingPost.ContentCs; break;
                                case "de": existingPost.TitleDe = existingPost.TitleCs; existingPost.ContentDe = existingPost.ContentCs; break;
                                case "es": existingPost.TitleEs = existingPost.TitleCs; existingPost.ContentEs = existingPost.ContentCs; break;
                                case "fr": existingPost.TitleFr = existingPost.TitleCs; existingPost.ContentFr = existingPost.ContentCs; break;
                                case "hi": existingPost.TitleHi = existingPost.TitleCs; existingPost.ContentHi = existingPost.ContentCs; break;
                                case "ja": existingPost.TitleJa = existingPost.TitleCs; existingPost.ContentJa = existingPost.ContentCs; break;
                                case "pt": existingPost.TitlePt = existingPost.TitleCs; existingPost.ContentPt = existingPost.ContentCs; break;
                                case "ru": existingPost.TitleRu = existingPost.TitleCs; existingPost.ContentRu = existingPost.ContentCs; break;
                                case "zh": existingPost.TitleZh = existingPost.TitleCs; existingPost.ContentZh = existingPost.ContentCs; break;
                            }
                        }
                    }
                    
                    _logger.LogInformation("All re-translations completed");

                    _logger.LogInformation("Re-translation completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeepL re-translation failed");
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = "Blog post updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog post {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the blog post.");
                return View(post);
            }
        }

        // GET: Admin/Blog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Admin/Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var post = await _db.BlogPosts.FindAsync(id);
                if (post == null)
                {
                    return NotFound();
                }

                _db.BlogPosts.Remove(post);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Blog post deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog post {Id}", id);
                TempData["Error"] = "An error occurred while deleting the blog post.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
