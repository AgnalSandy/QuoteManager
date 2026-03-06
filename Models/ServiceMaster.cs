using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class ServiceMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Service Charge (₹)")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal ServiceCharge { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Created By")]
        public string? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public ApplicationUser? CreatedBy { get; set; }

        // Concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation properties
        public ICollection<ServiceTax> ServiceTaxes { get; set; } = new List<ServiceTax>();
        public ICollection<QuoteItem> QuoteItems { get; set; } = new List<QuoteItem>();
    }
}
