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
            // Security: Get real IP (consider X-Forwarded-For for reverse proxy)
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            // Security: Rate limiting - 3 requests per 15 minutes per IP
            var key = $"inq:{ip}";
            var count = _cache.GetOrCreate<int>(key, e => { e.SlidingExpiration = TimeSpan.FromMinutes(15); return 0; });
            if (count >= 3)
            {
                return StatusCode(429, "Too many inquiries. Please try again later.");
            }

            // Security: Validate input lengths
            if (!string.IsNullOrWhiteSpace(model.Message) && model.Message.Length > 5000)
            {
                return BadRequest("Message is too long (max 5000 characters)");
            }

            model.CollectionId = collectionId;
            model.CollectionTitle = collectionTitle;
            model.OriginIp = ip;

            _cache.Set(key, count + 1, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) });

            try
            {
                _db.Inquiries.Add(model);
                await _db.SaveChangesAsync();
                await _email.SendInquiryAsync(model);
                return Ok(new { ok = true });
            }
            catch (Exception)
            {
                // Don't expose internal errors to client
                return StatusCode(500, "An error occurred while processing your inquiry");
            }
        }
    }
}