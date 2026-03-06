using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace QuoteManager.ViewModels
{
    public class CreateQuoteViewModel
    {
        [Required(ErrorMessage = "Please select a client")]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Quote Title")]
        public string? Title { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Valid Until")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        // Line Items
        public List<QuoteItemViewModel> Items { get; set; } = new();

        // Dropdowns
        public List<SelectListItem> AvailableClients { get; set; } = new();
        public List<SelectListItem> AvailableServices { get; set; } = new();
        public List<SelectListItem> AvailableTaxes { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for editing quotes - only allows editing specific safe fields
    /// </summary>
    public class EditQuoteViewModel
    {
        public int Id { get; set; }
        
        // Read-only fields (displayed but not editable)
        public string QuoteNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        
        // Editable fields only
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Quote Title")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Valid Until")]
        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = QuoteManager.Constants.QuoteStatus.Pending;

        // For display purposes
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class QuoteItemViewModel
    {
        public int? Id { get; set; }

        [Display(Name = "Service")]
        public int? ServiceId { get; set; }

        public string? ServiceName { get; set; }

        [Display(Name = "Is Custom Service")]
        public bool IsCustomService { get; set; } = false;

        [StringLength(200, ErrorMessage = "Custom service name cannot exceed 200 characters")]
        [Display(Name = "Custom Service Name")]
        public string? CustomServiceName { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? CustomDescription { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, 999999, ErrorMessage = "Quantity must be between 0.01 and 999,999")]
        public decimal Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 9999999999, ErrorMessage = "Unit price must be between 0.01 and 9,999,999,999")]
        [Display(Name = "Unit Price (₹)")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Amount (₹)")]
        public decimal Amount => Quantity * UnitPrice;

        // Taxes for this item
        public List<int> SelectedTaxIds { get; set; } = new();

        /// <summary>
        /// Validates business rules
        /// </summary>
        public void Validate()
        {
            if (IsCustomService)
            {
                if (string.IsNullOrWhiteSpace(CustomServiceName))
                    throw new ValidationException("Custom service name is required when using custom service");
            }
            else
            {
                if (!ServiceId.HasValue || ServiceId.Value <= 0)
                    throw new ValidationException("Please select a service");
            }

            if (Quantity <= 0)
                throw new ValidationException("Quantity must be greater than zero");

            if (UnitPrice <= 0)
                throw new ValidationException("Unit price must be greater than zero");
        }
    }

    public class QuoteDetailsViewModel
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public bool HasInvoice { get; set; }
        public int? InvoiceId { get; set; }

        public List<QuoteItemDetailViewModel> Items { get; set; } = new();
    }

    public class QuoteItemDetailViewModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string? CustomDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public List<TaxDetailViewModel> Taxes { get; set; } = new();
    }

    public class TaxDetailViewModel
    {
        public string TaxName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
