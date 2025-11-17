using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AAS.Web.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        [Required]
        public int CollectionId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Text { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public Collection? Collection { get; set; }
        public IdentityUser? User { get; set; }
    }
}
