using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAS.Web.Models
{
    public class Collection
    {
        public int Id { get; set; }
        [Required, MaxLength(180)] public string Title { get; set; } = string.Empty;
        [Required, MaxLength(200)] public string Slug { get; set; } = string.Empty;
        [Required] public CollectionCategory Category { get; set; }
        [Column(TypeName = "text")] public string Description { get; set; } = string.Empty;
        public string? AudioPath { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public ICollection<CollectionImage> Images { get; set; } = new List<CollectionImage>();
    }
}