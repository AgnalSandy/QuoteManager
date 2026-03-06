using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class QuoteItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuoteId { get; set; }

        [ForeignKey("QuoteId")]
        public Quote Quote { get; set; } = null!;

        // Service can be null for custom services
        public int? ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public ServiceMaster? Service { get; set; }

        // For custom services (when ServiceId is null)
        [StringLength(200)]
        public string? CustomServiceName { get; set; }

        [StringLength(1000)]
        public string? CustomDescription { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        // Navigation
        public ICollection<QuoteItemTax> QuoteItemTaxes { get; set; } = new List<QuoteItemTax>();
    }
}
