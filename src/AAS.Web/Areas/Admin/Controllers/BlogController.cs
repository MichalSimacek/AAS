using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<BlogController> _logger;

        public BlogController(
            AppDbContext db,
            IDeepLService deepL,
            UserManager<IdentityUser> userManager,
            ILogger<BlogController> logger)
        {
            _db = db;
            _deepL = deepL;
            _userManager = userManager;
            _logger = logger;
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
            if (!ModelState.IsValid)
            {
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

                // Handle featured image upload
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "blog");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{featuredImage.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await featuredImage.CopyToAsync(fileStream);
                    }

                    post.FeaturedImage = $"/uploads/blog/{uniqueFileName}";
                }

                // Translate title and content to all languages using DeepL
                // Uses automatic language detection - admin can write in any language
                try
                {
                    _logger.LogInformation("Starting DeepL translations for blog post...");

                    // Translate Title (auto-detect source language)
                    var titleTranslations = await _deepL.TranslateToAllLanguagesAsync(post.TitleCs, "auto");
                    post.TitleEn = titleTranslations.GetValueOrDefault("en", post.TitleCs);
                    post.TitleDe = titleTranslations.GetValueOrDefault("de", post.TitleCs);
                    post.TitleEs = titleTranslations.GetValueOrDefault("es", post.TitleCs);
                    post.TitleFr = titleTranslations.GetValueOrDefault("fr", post.TitleCs);
                    post.TitleHi = titleTranslations.GetValueOrDefault("hi", post.TitleCs);
                    post.TitleJa = titleTranslations.GetValueOrDefault("ja", post.TitleCs);
                    post.TitlePt = titleTranslations.GetValueOrDefault("pt", post.TitleCs);
                    post.TitleRu = titleTranslations.GetValueOrDefault("ru", post.TitleCs);
                    post.TitleZh = titleTranslations.GetValueOrDefault("zh", post.TitleCs);

                    // Translate Content (HTML from TinyMCE) - auto-detect source language
                    var contentTranslations = await _deepL.TranslateToAllLanguagesAsync(post.ContentCs, "auto");
                    post.ContentEn = contentTranslations.GetValueOrDefault("en", post.ContentCs);
                    post.ContentDe = contentTranslations.GetValueOrDefault("de", post.ContentCs);
                    post.ContentEs = contentTranslations.GetValueOrDefault("es", post.ContentCs);
                    post.ContentFr = contentTranslations.GetValueOrDefault("fr", post.ContentCs);
                    post.ContentHi = contentTranslations.GetValueOrDefault("hi", post.ContentCs);
                    post.ContentJa = contentTranslations.GetValueOrDefault("ja", post.ContentCs);
                    post.ContentPt = contentTranslations.GetValueOrDefault("pt", post.ContentCs);
                    post.ContentRu = contentTranslations.GetValueOrDefault("ru", post.ContentCs);
                    post.ContentZh = contentTranslations.GetValueOrDefault("zh", post.ContentCs);

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

                // Update basic fields
                existingPost.TitleCs = post.TitleCs;
                existingPost.ContentCs = post.ContentCs;
                existingPost.Published = post.Published;
                existingPost.UpdatedAt = DateTime.UtcNow;

                // Handle new featured image
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "blog");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{featuredImage.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await featuredImage.CopyToAsync(fileStream);
                    }

                    existingPost.FeaturedImage = $"/uploads/blog/{uniqueFileName}";
                }

                // Re-translate if content changed
                try
                {
                    _logger.LogInformation("Re-translating blog post {PostId}...", id);

                    var titleTranslations = await _deepL.TranslateToAllLanguagesAsync(existingPost.TitleCs, "cs");
                    existingPost.TitleEn = titleTranslations.GetValueOrDefault("en", existingPost.TitleCs);
                    existingPost.TitleDe = titleTranslations.GetValueOrDefault("de", existingPost.TitleCs);
                    existingPost.TitleEs = titleTranslations.GetValueOrDefault("es", existingPost.TitleCs);
                    existingPost.TitleFr = titleTranslations.GetValueOrDefault("fr", existingPost.TitleCs);
                    existingPost.TitleHi = titleTranslations.GetValueOrDefault("hi", existingPost.TitleCs);
                    existingPost.TitleJa = titleTranslations.GetValueOrDefault("ja", existingPost.TitleCs);
                    existingPost.TitlePt = titleTranslations.GetValueOrDefault("pt", existingPost.TitleCs);
                    existingPost.TitleRu = titleTranslations.GetValueOrDefault("ru", existingPost.TitleCs);
                    existingPost.TitleZh = titleTranslations.GetValueOrDefault("zh", existingPost.TitleCs);

                    var contentTranslations = await _deepL.TranslateToAllLanguagesAsync(existingPost.ContentCs, "cs");
                    existingPost.ContentEn = contentTranslations.GetValueOrDefault("en", existingPost.ContentCs);
                    existingPost.ContentDe = contentTranslations.GetValueOrDefault("de", existingPost.ContentCs);
                    existingPost.ContentEs = contentTranslations.GetValueOrDefault("es", existingPost.ContentCs);
                    existingPost.ContentFr = contentTranslations.GetValueOrDefault("fr", existingPost.ContentCs);
                    existingPost.ContentHi = contentTranslations.GetValueOrDefault("hi", existingPost.ContentCs);
                    existingPost.ContentJa = contentTranslations.GetValueOrDefault("ja", existingPost.ContentCs);
                    existingPost.ContentPt = contentTranslations.GetValueOrDefault("pt", existingPost.ContentCs);
                    existingPost.ContentRu = contentTranslations.GetValueOrDefault("ru", existingPost.ContentCs);
                    existingPost.ContentZh = contentTranslations.GetValueOrDefault("zh", existingPost.ContentCs);

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
