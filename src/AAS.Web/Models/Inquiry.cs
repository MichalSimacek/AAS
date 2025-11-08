using System.ComponentModel.DataAnnotations;

namespace AAS.Web.Models
{
    public class Inquiry
    {
        public int Id { get; set; }
        public int? CollectionId { get; set; }
        [MaxLength(200)] public string? CollectionTitle { get; set; }
        [Required, MaxLength(100)] public string? FirstName { get; set; }
        [Required, MaxLength(100)] public string? LastName { get; set; }
        [Required, EmailAddress, MaxLength(160)] public string? Email { get; set; }
        [Phone, MaxLength(40)] public string? Phone { get; set; }
        [MaxLength(5000)] public string? Message { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        [MaxLength(100)] public string OriginIp { get; set; } = string.Empty;
    }
}