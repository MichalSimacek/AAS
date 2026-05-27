using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAS.Web.Models
{
    public class Collection
    {
        public int Id { get; set; }
        [Required, MaxLength(180)] public string Title { get; set; } = string.Empty;
        [MaxLength(200)] public string Slug { get; set; } = string.Empty;
        // English slug — used for all non-Czech language URLs (variant C).
        // Auto-generated from the English translation when DeepL runs, or filled
        // by admin manually. Null/empty means the system will fall back to the
        // Czech Slug above.
        [MaxLength(200)] public string? SlugEn { get; set; }
        [Required] public CollectionCategory Category { get; set; }
        [Column(TypeName = "text"), MaxLength(10000)] public string Description { get; set; } = string.Empty;
        [MaxLength(500)] public string? AudioPath { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        
        // Price and availability
        public CollectionStatus Status { get; set; } = CollectionStatus.Available;
        // Changed from decimal to string to allow text values like "Price on request"
        [MaxLength(100)] public string? Price { get; set; }
        public Currency Currency { get; set; } = Currency.EUR;
        
        // AAS Verification - authenticity guaranteed by AAS
        public bool AASVerified { get; set; } = false;

        // Visibility - when true, the collection is hidden from public view (admin only)
        public bool IsHidden { get; set; } = false;
        
        public ICollection<CollectionImage> Images { get; set; } = new List<CollectionImage>();
        public ICollection<CollectionTranslation> Translations { get; set; } = new List<CollectionTranslation>();
    }
}