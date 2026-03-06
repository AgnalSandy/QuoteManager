using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class TaxMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]  // ⭐ ADD THIS LINE
        [Display(Name = "Tax Percentage (%)")]
        public decimal TaxPercentage { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public ApplicationUser? CreatedBy { get; set; }

        // Concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation property
        public ICollection<ServiceTax> ServiceTaxes { get; set; } = new List<ServiceTax>();
    }
}