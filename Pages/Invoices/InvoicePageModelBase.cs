using Microsoft.AspNetCore.Mvc.RazorPages;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Invoices
{
    /// <summary>
    /// Base class for invoice pages that need to display invoice templates
    /// </summary>
    public abstract class InvoicePageModelBase : PageModel
    {
        public InvoiceDetailsViewModel Invoice { get; set; } = new InvoiceDetailsViewModel();
        public CompanySettingsViewModel Company { get; set; } = new CompanySettingsViewModel();
    }
}
