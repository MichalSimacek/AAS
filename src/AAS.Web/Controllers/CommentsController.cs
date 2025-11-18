using AAS.Web.Data;
using AAS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AAS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(AppDbContext db, ILogger<CommentsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("collection/{collectionId}")]
        public async Task<IActionResult> GetComments(int collectionId)
        {
            try
            {
                var comments = await _db.Comments
                    .Include(c => c.User)
                    .Where(c => c.CollectionId == collectionId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Text,
                        c.CreatedAt,
                        c.UpdatedAt,
                        c.UserId,
                        UserName = c.User != null ? c.User.UserName : "Unknown"
                    })
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} comments for collection {CollectionId}", comments.Count, collectionId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for collection {CollectionId}", collectionId);
                return StatusCode(500, new { error = "Failed to fetch comments" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] int collectionId, [FromForm] string text)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
                    return BadRequest(new { error = "Invalid comment text" });

                var collection = await _db.Collections.FindAsync(collectionId);
                if (collection == null)
                    return NotFound(new { error = "Collection not found" });

                var comment = new Comment
                {
                    CollectionId = collectionId,
                    UserId = userId,
                    Text = text.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Comments.Add(comment);
                await _db.SaveChangesAsync();

                var user = await _db.Users.FindAsync(userId);
                return Ok(new
                {
                    comment.Id,
                    comment.Text,
                    comment.CreatedAt,
                    UserName = user?.UserName ?? "Unknown"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new { error = "Failed to create comment" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [FromForm] string text)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var comment = await _db.Comments.FindAsync(id);
                if (comment == null)
                    return NotFound(new { error = "Comment not found" });

                // Check if user is owner or admin
                var isAdmin = User.IsInRole("Admin");
                if (comment.UserId != userId && !isAdmin)
                    return Forbid();

                if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
                    return BadRequest(new { error = "Invalid comment text" });

                comment.Text = text.Trim();
                comment.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new { comment.Id, comment.Text, comment.UpdatedAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {Id}", id);
                return StatusCode(500, new { error = "Failed to update comment" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var comment = await _db.Comments.FindAsync(id);
                if (comment == null)
                    return NotFound(new { error = "Comment not found" });

                // Check if user is owner or admin
                var isAdmin = User.IsInRole("Admin");
                if (comment.UserId != userId && !isAdmin)
                    return Forbid();

                _db.Comments.Remove(comment);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Comment deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {Id}", id);
                return StatusCode(500, new { error = "Failed to delete comment" });
            }
        }
    }
}
