using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.ServiceMasters
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public EditServiceViewModel Input { get; set; } = new EditServiceViewModel();

        public List<SelectListItem> AvailableTaxes { get; set; } = new List<SelectListItem>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.ServiceMasters
                .Include(s => s.ServiceTaxes)
                .Include(s => s.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Authorization check: Admin can only edit their own services
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(ApplicationRoles.Admin) && service.CreatedById != currentUser?.Id)
            {
                TempData[TempDataKeys.Error] = "You can only edit services that you created.";
                return RedirectToPage("./Index");
            }

            Input = new EditServiceViewModel
            {
                Id = service.Id,
                ServiceName = service.ServiceName,
                Description = service.Description,
                ServiceCharge = service.ServiceCharge,
                IsActive = service.IsActive,
                SelectedTaxIds = service.ServiceTaxes.Select(st => st.TaxId).ToList()
            };

            await LoadTaxes();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTaxes();
                return Page();
            }

            var service = await _context.ServiceMasters
                .Include(s => s.ServiceTaxes)
                .FirstOrDefaultAsync(s => s.Id == Input.Id);

            if (service == null)
            {
                return NotFound();
            }

            // Authorization check on POST as well
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(ApplicationRoles.Admin) && service.CreatedById != currentUser?.Id)
            {
                TempData[TempDataKeys.Error] = "You can only edit services that you created.";
                return RedirectToPage("./Index");
            }

            service.ServiceName = Input.ServiceName;
            service.Description = Input.Description;
            service.ServiceCharge = Input.ServiceCharge;
            service.IsActive = Input.IsActive;

            // Remove old tax mappings
            _context.ServiceTaxes.RemoveRange(service.ServiceTaxes);

            // Add new tax mappings
            if (Input.SelectedTaxIds.Any())
            {
                foreach (var taxId in Input.SelectedTaxIds)
                {
                    service.ServiceTaxes.Add(new Models.ServiceTax
                    {
                        ServiceId = service.Id,
                        TaxId = taxId
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Service '{service.ServiceName}' updated successfully!";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(Input.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        private bool ServiceExists(int id)
        {
            return _context.ServiceMasters.Any(s => s.Id == id);
        }

        private async Task LoadTaxes()
        {
            AvailableTaxes = await _context.TaxMasters
                .Where(t => t.IsActive)
                .OrderBy(t => t.TaxName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.TaxName} ({t.TaxPercentage}%)"
                })
                .ToListAsync();
        }
    }
}
