using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.TaxMasters
{
    [Authorize(Roles = "SuperAdmin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EditTaxViewModel Input { get; set; } = new EditTaxViewModel();

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

            Input = new EditTaxViewModel
            {
                Id = tax.Id,
                TaxName = tax.TaxName,
                TaxPercentage = tax.TaxPercentage,
                Description = tax.Description,
                IsActive = tax.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var tax = await _context.TaxMasters.FindAsync(Input.Id);

            if (tax == null)
            {
                return NotFound();
            }

            tax.TaxName = Input.TaxName;
            tax.TaxPercentage = Input.TaxPercentage;
            tax.Description = Input.Description;
            tax.IsActive = Input.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tax '{tax.TaxName}' updated successfully!";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaxExists(Input.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        private bool TaxExists(int id)
        {
            return _context.TaxMasters.Any(t => t.Id == id);
        }
    }
}
