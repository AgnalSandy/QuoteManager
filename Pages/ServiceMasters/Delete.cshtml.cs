using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.ServiceMasters
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ServiceMasterViewModel Service { get; set; } = new ServiceMasterViewModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.ServiceMasters
                .Include(s => s.ServiceTaxes)
                    .ThenInclude(st => st.Tax)
                .Include(s => s.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Authorization check: Admin can only delete their own services
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(ApplicationRoles.Admin) && service.CreatedById != currentUser?.Id)
            {
                TempData[TempDataKeys.Error] = "You can only delete services that you created.";
                return RedirectToPage("./Index");
            }

            Service = new ServiceMasterViewModel
            {
                Id = service.Id,
                ServiceName = service.ServiceName,
                Description = service.Description,
                ServiceCharge = service.ServiceCharge,
                IsActive = service.IsActive,
                ApplicableTaxes = string.Join(", ", service.ServiceTaxes.Select(st => $"{st.Tax.TaxName} ({st.Tax.TaxPercentage}%)"))
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.ServiceMasters
                .Include(s => s.QuoteItems)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            // Authorization check on POST
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole(ApplicationRoles.Admin) && service.CreatedById != currentUser?.Id)
            {
                TempData[TempDataKeys.Error] = "You can only delete services that you created.";
                return RedirectToPage("./Index");
            }

            // Check if service is used in any quotes
            if (service.QuoteItems.Any())
            {
                TempData[TempDataKeys.Error] = $"Cannot delete '{service.ServiceName}' because it is used in {service.QuoteItems.Count} quote(s).";
                return RedirectToPage("./Index");
            }

            _context.ServiceMasters.Remove(service);
            await _context.SaveChangesAsync();

            TempData[TempDataKeys.Success] = $"Service '{service.ServiceName}' deleted successfully!";
            return RedirectToPage("./Index");
        }
    }
}
