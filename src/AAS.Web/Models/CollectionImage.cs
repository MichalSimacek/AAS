using System.ComponentModel.DataAnnotations;

namespace AAS.Web.Models
{
    public class CollectionImage
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public Collection? Collection { get; set; }
        [Required, MaxLength(100)] public string FileName { get; set; } = string.Empty; // original
        [Range(1, 10000)] public int Width { get; set; }
        [Range(1, 10000)] public int Height { get; set; }
        [Range(1, long.MaxValue)] public long Bytes { get; set; }
        [Range(0, int.MaxValue)] public int SortOrder { get; set; }
    }
}