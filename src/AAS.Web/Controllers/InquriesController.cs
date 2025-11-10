using AAS.Web.Data;
using AAS.Web.Models;
using AAS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;


namespace AAS.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class InquiriesController : Controller
    {
        private readonly AppDbContext _db; private readonly EmailService _email; private readonly IMemoryCache _cache;
        public InquiriesController(AppDbContext db, EmailService email, IMemoryCache cache) { _db = db; _email = email; _cache = cache; }


        [HttpPost]
        public async Task<IActionResult> Create(int? collectionId, string? collectionTitle, Inquiry model)
        {
            // DEBUG: Log received data
            Console.WriteLine($"[INQUIRY DEBUG] Received: CollectionId={collectionId}, Title={collectionTitle}");
            Console.WriteLine($"[INQUIRY DEBUG] Model: Name={model.Name}, Email={model.Email}, Phone={model.Phone}, Message={model.Message}");
            
            // SECURITY: Validate model state (includes all data annotations)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"[INQUIRY DEBUG] Validation failed: {string.Join(", ", errors)}");
                return BadRequest(new { success = false, message = "Invalid input data", errors = errors });
            }

            // Security: Get real IP (validate X-Forwarded-For header)
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // SECURITY: Validate forwarded IP format to prevent header injection
            string ip = remoteIp;
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                var firstIp = ips[0].Trim();
                // Basic IP validation
                if (System.Net.IPAddress.TryParse(firstIp, out _))
                {
                    ip = firstIp;
                }
            }

            // Security: Rate limiting - 3 requests per 15 minutes per IP
            var key = $"inq:{ip}";
            var count = _cache.GetOrCreate<int>(key, e => { e.SlidingExpiration = TimeSpan.FromMinutes(15); return 0; });
            if (count >= 3)
            {
                return StatusCode(429, new { success = false, message = "Too many inquiries. Please try again later." });
            }

            // SECURITY: Validate collectionId if provided
            if (collectionId.HasValue && collectionId.Value <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid collection ID" });
            }

            // SECURITY: Sanitize and validate collectionTitle length
            if (!string.IsNullOrWhiteSpace(collectionTitle) && collectionTitle.Length > 200)
            {
                collectionTitle = collectionTitle.Substring(0, 200);
            }

            model.CollectionId = collectionId;
            model.CollectionTitle = collectionTitle;
            model.OriginIp = ip.Length > 100 ? ip.Substring(0, 100) : ip;

            _cache.Set(key, count + 1, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) });

            try
            {
                _db.Inquiries.Add(model);
                await _db.SaveChangesAsync();
                await _email.SendInquiryAsync(model);
                return Ok(new { success = true, message = "Inquiry submitted successfully" });
            }
            catch (Exception ex)
            {
                // SECURITY: Log error securely without exposing details to client
                Console.WriteLine($"[SECURE] Inquiry submission error: {ex.GetType().Name}");
                return StatusCode(500, new { success = false, message = "An error occurred while processing your inquiry" });
            }
        }
    }
}