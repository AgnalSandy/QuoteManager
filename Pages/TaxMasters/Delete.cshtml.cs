using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.TaxMasters
{
    [Authorize(Roles = "SuperAdmin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TaxMasterViewModel Tax { get; set; } = new TaxMasterViewModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tax = await _context.TaxMasters.FindAsync(id);

            if (tax == null)
            {
                return NotFound();
            }

            Tax = new TaxMasterViewModel
            {
                Id = tax.Id,
                TaxName = tax.TaxName,
                TaxPercentage = tax.TaxPercentage,
                Description = tax.Description,
                IsActive = tax.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tax = await _context.TaxMasters
                .Include(t => t.ServiceTaxes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tax == null)
            {
                return NotFound();
            }

            // Check if tax is used in any services
            if (tax.ServiceTaxes.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{tax.TaxName}' because it is assigned to {tax.ServiceTaxes.Count} service(s).";
                return RedirectToPage("./Index");
            }

            _context.TaxMasters.Remove(tax);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tax '{tax.TaxName}' deleted successfully!";
            return RedirectToPage("./Index");
        }
    }
}
