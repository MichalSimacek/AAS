using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AAS.Web.Services
{
    public class ImageService
    {
        private readonly IWebHostEnvironment _env; private readonly IConfiguration _cfg;
        public ImageService(IWebHostEnvironment env, IConfiguration cfg) { _env = env; _cfg = cfg; }

        public async Task<(int w, int h, long b)> SaveOriginalAndVariantsAsync(IFormFile file, string fileNameNoExt)
        {
            // Security: Validate file type
            var allowedExtensions = _cfg.GetSection("Uploads:AllowedImageExtensions").Get<string[]>()
                                    ?? new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException($"File type {ext} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            // Security: Validate file size
            var maxSizeMB = int.Parse(_cfg["Uploads:MaxImageSizeMB"] ?? "10");
            var maxBytes = maxSizeMB * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {maxSizeMB}MB");
            }

            if (file.Length == 0)
            {
                throw new InvalidOperationException("File is empty");
            }

            var root = Path.Combine(_env.ContentRootPath, _cfg["Uploads:ImagesPath"]!);
            Directory.CreateDirectory(root);

            // Security: Generate safe filename (no user input in path)
            var safeName = fileNameNoExt + ext;
            var originalPath = Path.Combine(root, safeName);

            // PERFORMANCE: Use larger buffer for better I/O performance
            await using (var fs = new FileStream(originalPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await file.CopyToAsync(fs);
            }

            // Security: Validate that file is actually an image by loading it
            try
            {
                using var img = await Image.LoadAsync(originalPath);
                var (w, h) = (img.Width, img.Height);

                // Security: Enforce max dimensions
                const int maxDimension = 8000;
                if (w > maxDimension || h > maxDimension)
                {
                    CleanupFiles(root, fileNameNoExt);
                    throw new InvalidOperationException($"Image dimensions exceed maximum allowed size of {maxDimension}px");
                }

                await SaveVariantAsync(img, root, fileNameNoExt, 1600);
                await SaveVariantAsync(img, root, fileNameNoExt, 960);
                await SaveVariantAsync(img, root, fileNameNoExt, 480);
                var bytes = new FileInfo(originalPath).Length;
                return (w, h, bytes);
            }
            catch (Exception ex)
            {
                // CRITICAL: Clean up ALL files on error (original + variants)
                CleanupFiles(root, fileNameNoExt);
                throw new InvalidOperationException("File is not a valid image", ex);
            }
        }

        private static void CleanupFiles(string root, string nameNoExt)
        {
            try
            {
                // SECURITY: Validate filename to prevent path traversal
                if (string.IsNullOrWhiteSpace(nameNoExt) || 
                    nameNoExt.Contains("..") || 
                    nameNoExt.Contains("/") || 
                    nameNoExt.Contains("\\"))
                {
                    return;
                }

                // Delete original and all variants
                var rootPath = Path.GetFullPath(root);
                foreach (var file in Directory.GetFiles(root, $"{nameNoExt}*"))
                {
                    // SECURITY: Verify file is within uploads directory
                    var fullPath = Path.GetFullPath(file);
                    if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public void DeleteAllVariants(string fileNameNoExt)
        {
            try
            {
                // SECURITY: Validate filename to prevent path traversal
                if (string.IsNullOrWhiteSpace(fileNameNoExt) || 
                    fileNameNoExt.Contains("..") || 
                    fileNameNoExt.Contains("/") || 
                    fileNameNoExt.Contains("\\"))
                {
                    throw new InvalidOperationException("Invalid filename");
                }

                var root = Path.Combine(_env.ContentRootPath, _cfg["Uploads:ImagesPath"]!);
                if (Directory.Exists(root))
                {
                    // Delete original and all variants (480, 960, 1600)
                    foreach (var file in Directory.GetFiles(root, $"{fileNameNoExt}*"))
                    {
                        // SECURITY: Verify file is within uploads directory
                        var fullPath = Path.GetFullPath(file);
                        var rootPath = Path.GetFullPath(root);
                        if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // SECURITY: Log but don't expose file system details
                Console.WriteLine($"[SECURE] Image deletion failed: {ex.GetType().Name}");
            }
        }

        private static async Task SaveVariantAsync(Image image, string root, string name, int width)
        {
            using var clone = image.Clone(ctx => ctx.AutoOrient().Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width, 0)
            }));
            var enc = new JpegEncoder { Quality = 90 };
            await clone.SaveAsync(Path.Combine(root, $"{name}-{width}.jpg"), enc);
        }
    }
}