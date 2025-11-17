using System.ComponentModel.DataAnnotations;

namespace AAS.Web.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        
        // Multilingual titles
        [Required] [StringLength(200)] public string TitleCs { get; set; } = string.Empty;
        [StringLength(200)] public string? TitleEn { get; set; }
        [StringLength(200)] public string? TitleDe { get; set; }
        [StringLength(200)] public string? TitleEs { get; set; }
        [StringLength(200)] public string? TitleFr { get; set; }
        [StringLength(200)] public string? TitleHi { get; set; }
        [StringLength(200)] public string? TitleJa { get; set; }
        [StringLength(200)] public string? TitlePt { get; set; }
        [StringLength(200)] public string? TitleRu { get; set; }
        [StringLength(200)] public string? TitleZh { get; set; }
        
        // Multilingual content (HTML from TinyMCE)
        [Required] public string ContentCs { get; set; } = string.Empty;
        public string? ContentEn { get; set; }
        public string? ContentDe { get; set; }
        public string? ContentEs { get; set; }
        public string? ContentFr { get; set; }
        public string? ContentHi { get; set; }
        public string? ContentJa { get; set; }
        public string? ContentPt { get; set; }
        public string? ContentRu { get; set; }
        public string? ContentZh { get; set; }
        
        [StringLength(500)]
        public string? FeaturedImage { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string AuthorId { get; set; } = string.Empty;
        
        public bool Published { get; set; } = false;
    }
}
