using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAS.Web.Models
{
    public class CollectionTranslation
    {
        public int Id { get; set; }

        [Required]
        public int CollectionId { get; set; }

        public Collection Collection { get; set; } = null!;

        [Required, MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty; // "cs", "de", "es", etc.

        [Required, MaxLength(200)]
        public string TranslatedTitle { get; set; } = string.Empty;

        [Column(TypeName = "text"), MaxLength(10000)]
        public string TranslatedDescription { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedUtc { get; set; }
    }
}
