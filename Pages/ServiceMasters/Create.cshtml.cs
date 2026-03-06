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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public CreateServiceViewModel Input { get; set; } = new CreateServiceViewModel();

        public List<SelectListItem> AvailableTaxes { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync()
        {
            await LoadTaxes();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTaxes();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var service = new ServiceMaster
            {
                ServiceName = Input.ServiceName,
                Description = Input.Description,
                ServiceCharge = Input.ServiceCharge,
                IsActive = Input.IsActive,
                CreatedById = currentUser?.Id,
                CreatedDate = DateTime.UtcNow
            };

            _context.ServiceMasters.Add(service);
            await _context.SaveChangesAsync();

            // Add selected taxes
            if (Input.SelectedTaxIds.Any())
            {
                foreach (var taxId in Input.SelectedTaxIds)
                {
                    var serviceTax = new ServiceTax
                    {
                        ServiceId = service.Id,
                        TaxId = taxId
                    };
                    _context.ServiceTaxes.Add(serviceTax);
                }
                await _context.SaveChangesAsync();
            }

            TempData[TempDataKeys.Success] = $"Service '{service.ServiceName}' created successfully!";
            return RedirectToPage("./Index");
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
