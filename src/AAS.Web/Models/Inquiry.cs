using System.ComponentModel.DataAnnotations;

namespace AAS.Web.Models
{
    public class Inquiry
    {
        public int Id { get; set; }
        public int? CollectionId { get; set; }
        public string? CollectionTitle { get; set; }
        [MaxLength(100)] public string? FirstName { get; set; }
        [MaxLength(100)] public string? LastName { get; set; }
        [EmailAddress, MaxLength(160)] public string? Email { get; set; }
        [MaxLength(40)] public string? Phone { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public string OriginIp { get; set; } = string.Empty;
    }
}