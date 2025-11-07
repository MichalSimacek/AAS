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
        private readonly AppDbContext _db; private readonly SlugService _slug; private readonly ImageService _img;
        public CollectionsController(AppDbContext db, SlugService slug, ImageService img) { _db = db; _slug = slug; _img = img; }

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
            model.Slug = _slug.ToSlug(model.Title);

            // Security: Validate audio file
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

                var audioDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/audio");
                Directory.CreateDirectory(audioDir);
                var audioName = Guid.NewGuid().ToString("N") + audioExt;
                var audioPath = Path.Combine(audioDir, audioName);

                // CRITICAL FIX: Use await using to properly dispose FileStream
                await using (var fs = new FileStream(audioPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                {
                    await audio.CopyToAsync(fs);
                }

                model.AudioPath = "/uploads/audio/" + audioName;
            }

            // Security: Validate at least one image is provided
            if (!images.Any(f => f.Length > 0))
            {
                ModelState.AddModelError("images", "At least one image is required");
                return View(model);
            }

            // CRITICAL FIX: Use transaction to ensure data consistency
            // If image upload fails, rollback the collection creation
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Collections.Add(model);
                await _db.SaveChangesAsync();

                int order = 0;
                foreach (var f in images.Where(f => f.Length > 0))
                {
                    var nameNoExt = Guid.NewGuid().ToString("N");
                    var meta = await _img.SaveOriginalAndVariantsAsync(f, nameNoExt);
                    var imgEntity = new CollectionImage
                    {
                        CollectionId = model.Id,
                        FileName = nameNoExt,
                        Width = meta.w,
                        Height = meta.h,
                        Bytes = meta.b,
                        SortOrder = order++
                    };
                    _db.CollectionImages.Add(imgEntity);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Collection '{model.Title}' created successfully!";
                return RedirectToAction(nameof(Index), new { area = "Admin" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("images", $"Error uploading images: {ex.Message}");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Collections.Include(c => c.Images.OrderBy(i => i.SortOrder)).FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB max
        public async Task<IActionResult> Edit(int id, Collection model, List<IFormFile> newImages)
        {
            var existing = await _db.Collections.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();

            // CRITICAL FIX: Use transaction for data consistency
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                existing.Title = model.Title;
                existing.Description = model.Description;
                existing.Category = model.Category;
                existing.Slug = _slug.ToSlug(model.Title);

                // Process new images if any
                if (newImages != null && newImages.Any(f => f.Length > 0))
                {
                    int order = existing.Images.Count == 0 ? 0 : existing.Images.Max(i => i.SortOrder) + 1;
                    foreach (var f in newImages.Where(f => f.Length > 0))
                    {
                        var nameNoExt = Guid.NewGuid().ToString("N");
                        var meta = await _img.SaveOriginalAndVariantsAsync(f, nameNoExt);
                        _db.CollectionImages.Add(new CollectionImage
                        {
                            CollectionId = existing.Id,
                            FileName = nameNoExt,
                            Width = meta.w,
                            Height = meta.h,
                            Bytes = meta.b,
                            SortOrder = order++
                        });
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("newImages", ex.Message);
                return View(existing);
            }
        }
    }
}