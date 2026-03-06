using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.TaxMasters
{
    [Authorize(Roles = "SuperAdmin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CreateTaxViewModel Input { get; set; } = new CreateTaxViewModel();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var tax = new TaxMaster
            {
                TaxName = Input.TaxName,
                TaxPercentage = Input.TaxPercentage,
                Description = Input.Description,
                IsActive = Input.IsActive
            };

            _context.TaxMasters.Add(tax);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tax '{tax.TaxName}' created successfully!";
            return RedirectToPage("./Index");
        }
    }
}
