using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuoteManager.ViewModels
{
    public class InvoiceListViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Client Name")]
        public string ClientName { get; set; } = string.Empty;

        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; }

        [Display(Name = "Grand Total (₹)")]
        public decimal GrandTotal { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Template")]
        public string TemplateType { get; set; } = string.Empty;
    }

    public class CreateInvoiceViewModel
    {
        [Required(ErrorMessage = "Quote is required")]
        [Display(Name = "Select Quote")]
        public int QuoteId { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [Display(Name = "Discount (₹)")]
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;

        // ⭐ Template Selection
        [Required(ErrorMessage = "Please select a template")]
        [Display(Name = "Invoice Template")]
        public string TemplateType { get; set; } = "Professional";

        [StringLength(200)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string? AccountName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Dropdown options
        public List<SelectListItem>? AvailableQuotes { get; set; }
        public List<SelectListItem>? AvailableTemplates { get; set; }
    }


    public class InvoiceDetailsViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public string QuoteTitle { get; set; } = string.Empty;
        public string QuoteNumber { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal VATTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? BankName { get; set; }
        public string? AccountName { get; set; }
        public string? SwiftAddress { get; set; }
        public string? AccountNumber { get; set; }
        public string? IBANNumber { get; set; }
        public string? Notes { get; set; }

        public List<InvoiceItemViewModel> Items { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public List<InvoiceTaxItemViewModel> Taxes { get; set; } = new();
    }

    public class InvoiceTaxItemViewModel
    {
        public string TaxName { get; set; } = string.Empty;
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
    }

    public class EditInvoiceViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Discount (₹)")]
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;

        [Required(ErrorMessage = "Please select a template")]
        [Display(Name = "Invoice Template")]
        public string TemplateType { get; set; } = "Professional";

        [Required(ErrorMessage = "Please select a status")]
        [Display(Name = "Payment Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Paid Date")]
        public DateTime? PaidDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string? AccountName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "SWIFT Address")]
        public string? SwiftAddress { get; set; }

        [StringLength(50)]
        [Display(Name = "IBAN Number")]
        public string? IBANNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // For display only (not editable)
        public string ClientName { get; set; } = string.Empty;
        public string QuoteNumber { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal VATTotal { get; set; }
        public decimal GrandTotal { get; set; }

        // Dropdown options
        public List<SelectListItem>? AvailableTemplates { get; set; }
        public List<SelectListItem>? AvailableStatuses { get; set; }

        // For concurrency control
        public byte[]? RowVersion { get; set; }
    }
}
