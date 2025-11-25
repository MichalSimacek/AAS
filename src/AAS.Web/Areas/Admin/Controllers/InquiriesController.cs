using AAS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AAS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InquiriesController : Controller
    {
        private readonly AppDbContext _db;

        public InquiriesController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var inquiries = await _db.Inquiries
                .OrderByDescending(i => i.CreatedUtc)
                .AsNoTracking()
                .ToListAsync();

            return View(inquiries);
        }
    }
}
