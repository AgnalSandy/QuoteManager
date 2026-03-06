using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class CompanySettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Address Line 1")]
        public string? AddressLine1 { get; set; }

        [StringLength(500)]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        [Display(Name = "PIN Code")]
        public string? PinCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "India";

        [StringLength(20)]
        [Display(Name = "Primary Phone")]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Secondary Phone")]
        public string? SecondaryPhone { get; set; }

        [StringLength(20)]
        [Display(Name = "WhatsApp Number")]
        public string? WhatsAppNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        [Display(Name = "GST Number")]
        public string? GSTNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "PAN Number")]
        public string? PANNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Footer Message")]
        public string? FooterMessage { get; set; } = "Thank you for your business!";

        [StringLength(500)]
        public string? LogoPath { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string? UpdatedById { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }
    }
}