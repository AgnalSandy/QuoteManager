using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class Quote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string QuoteNumber { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; } = QuoteStatus.Pending;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ValidUntil { get; set; }

        // Relationships
        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public ApplicationUser? CreatedBy { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [ForeignKey("ClientId")]
        public ApplicationUser? Client { get; set; }
    }

    public static class QuoteStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";
    }
}
