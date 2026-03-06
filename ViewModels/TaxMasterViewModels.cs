using System.ComponentModel.DataAnnotations;

namespace QuoteManager.ViewModels
{
    // For displaying tax in a list
    public class TaxMasterViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = string.Empty;

        [Display(Name = "Tax Percentage")]
        public decimal TaxPercentage { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    // For creating a new tax
    public class CreateTaxViewModel
    {
        [Required(ErrorMessage = "Tax name is required")]
        [StringLength(50)]
        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax percentage is required")]
        [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100")]
        [Display(Name = "Tax Percentage (%)")]
        public decimal TaxPercentage { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    // For editing existing tax
    public class EditTaxViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tax name is required")]
        [StringLength(50)]
        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax percentage is required")]
        [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100")]
        [Display(Name = "Tax Percentage (%)")]
        public decimal TaxPercentage { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
