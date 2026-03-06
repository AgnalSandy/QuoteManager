using System.ComponentModel.DataAnnotations;

namespace QuoteManager.ViewModels
{
    // For displaying service in a list
    public class ServiceMasterViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Service Charge (₹)")]
        public decimal ServiceCharge { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Applicable Taxes")]
        public string ApplicableTaxes { get; set; } = string.Empty;

        [Display(Name = "Created By")]
        public string? CreatedByName { get; set; }
    }

    // For creating a new service
    public class CreateServiceViewModel
    {
        [Required(ErrorMessage = "Service name is required")]
        [StringLength(100)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service charge is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Service charge must be greater than 0")]
        [Display(Name = "Service Charge (₹)")]
        public decimal ServiceCharge { get; set; }

        [Display(Name = "Applicable Taxes")]
        public List<int> SelectedTaxIds { get; set; } = new List<int>();

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    // For editing existing service
    public class EditServiceViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service name is required")]
        [StringLength(100)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service charge is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Service charge must be greater than 0")]
        [Display(Name = "Service Charge (₹)")]
        public decimal ServiceCharge { get; set; }

        [Display(Name = "Applicable Taxes")]
        public List<int> SelectedTaxIds { get; set; } = new List<int>();

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
