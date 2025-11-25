using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

            // Get recent activity (last 20 items)
            var recentActivities = new List<ActivityItem>();

            // Recent comments
            var comments = await _db.Comments
                .Include(c => c.Collection)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            foreach (var comment in comments)
            {
                recentActivities.Add(new ActivityItem
                {
                    Type = "comment",
                    Message = $"{comment.User?.Email ?? "Anonymous"} commented on {comment.Collection?.Title ?? "collection"}",
                    Timestamp = comment.CreatedAt,
                    Icon = "chat-left-text",
                    Color = "#1976D2"
                });
            }

            // Recent inquiries
            var inquiries = await _db.Inquiries
                .OrderByDescending(i => i.CreatedUtc)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            foreach (var inquiry in inquiries)
            {
                recentActivities.Add(new ActivityItem
                {
                    Type = "inquiry",
                    Message = $"{inquiry.FirstName} {inquiry.LastName} sent an inquiry about {inquiry.CollectionTitle ?? "general"}",
                    Timestamp = inquiry.CreatedUtc,
                    Icon = "envelope",
                    Color = "#EF6C00"
                });
            }

            // Recent user registrations (last 7 days)
            var users = await _userManager.Users.ToListAsync();
            var recentUsers = users
                .Where(u => u.LockoutEnd == null) // Filter out locked users
                .OrderByDescending(u => u.Id)
                .Take(10)
                .ToList();

            foreach (var user in recentUsers)
            {
                recentActivities.Add(new ActivityItem
                {
                    Type = "registration",
                    Message = $"New user registered: {user.Email}",
                    Timestamp = DateTime.UtcNow.AddDays(-7), // Placeholder, as IdentityUser doesn't have CreatedAt
                    Icon = "person-plus",
                    Color = "#2E7D32"
                });
            }

            // Sort all activities by timestamp and take top 20
            ViewBag.RecentActivities = recentActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .ToList();

            return View(recentCollections);
        }
    }
}
