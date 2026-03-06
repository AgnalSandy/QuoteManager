using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace QuoteManager.ViewModels
{
    /// <summary>
    /// Secure ViewModel for editing quotes - prevents mass assignment
    /// </summary>
    public class SecureEditQuoteViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "Quote Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Valid Until")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        // Read-only properties for display
        public string QuoteNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<SelectListItem> AvailableStatuses { get; set; } = new();
    }
}
