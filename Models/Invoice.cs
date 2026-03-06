using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int QuoteId { get; set; }

        [ForeignKey("QuoteId")]
        public Quote Quote { get; set; } = null!;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [ForeignKey("ClientId")]
        public ApplicationUser Client { get; set; } = null!;

        [Required]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Sub Total (₹)")]
        public decimal SubTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "VAT/Tax Total (₹)")]
        public decimal VATTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount (₹)")]
        public decimal Discount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Grand Total (₹)")]
        public decimal GrandTotal { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = Constants.InvoiceStatus.Unpaid;

        public DateTime? PaidDate { get; set; }

        // ⭐ NEW: Multi-Template Support
        [Required]
        [StringLength(50)]
        [Display(Name = "Template Type")]
        public string TemplateType { get; set; } = "Professional";

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Bank Details (for invoice footer)
        [StringLength(200)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string? AccountName { get; set; }

        [StringLength(50)]
        [Display(Name = "SWIFT Address")]
        public string? SwiftAddress { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "IBAN Number")]
        public string? IBANNumber { get; set; }

        // Who created this invoice
        public string? PreparedById { get; set; }

        [ForeignKey("PreparedById")]
        public ApplicationUser? PreparedBy { get; set; }

        // Concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Computed property
        [NotMapped]
        public bool IsPaid => Status == Constants.InvoiceStatus.Paid;
    }
}
