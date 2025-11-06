namespace AAS.Web.Models
{
    public class CollectionImage
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public Collection? Collection { get; set; }
        public string FileName { get; set; } = string.Empty; // original
        public int Width { get; set; }
        public int Height { get; set; }
        public long Bytes { get; set; }
        public int SortOrder { get; set; }
    }
}