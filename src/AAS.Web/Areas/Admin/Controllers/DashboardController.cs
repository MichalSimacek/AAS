using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Get statistics
            var totalCollections = await _db.Collections.CountAsync();
            var availableCollections = await _db.Collections.CountAsync(c => c.Status == CollectionStatus.Available);
            var soldCollections = await _db.Collections.CountAsync(c => c.Status == CollectionStatus.Sold);
            var inAuctionCollections = await _db.Collections.CountAsync(c => c.Status == CollectionStatus.InAuction);
            var totalInquiries = await _db.Inquiries.CountAsync();
            var recentInquiries = await _db.Inquiries.CountAsync(i => i.CreatedUtc >= DateTime.UtcNow.AddDays(-7));

            ViewBag.TotalCollections = totalCollections;
            ViewBag.AvailableCollections = availableCollections;
            ViewBag.SoldCollections = soldCollections;
            ViewBag.InAuctionCollections = inAuctionCollections;
            ViewBag.TotalInquiries = totalInquiries;
            ViewBag.RecentInquiries = recentInquiries;

            // Get recent collections (last 5)
            var recentCollections = await _db.Collections
                .Include(c => c.Images.OrderBy(i => i.SortOrder).Take(1))
                .OrderByDescending(c => c.CreatedUtc)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            return View(recentCollections);
        }
    }
}
