using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.ServiceMasters
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ServiceMasterViewModel> Services { get; set; } = new List<ServiceMasterViewModel>();

        public async Task OnGetAsync()
        {
            Services = await _context.ServiceMasters
                .Include(s => s.ServiceTaxes)
                    .ThenInclude(st => st.Tax)
                .Include(s => s.CreatedBy)
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceMasterViewModel
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    ServiceCharge = s.ServiceCharge,
                    IsActive = s.IsActive,
                    ApplicableTaxes = string.Join(", ", s.ServiceTaxes.Select(st => $"{st.Tax.TaxName} ({st.Tax.TaxPercentage}%)")),
                    CreatedByName = s.CreatedBy != null ? s.CreatedBy.FullName : "System"
                })
                .ToListAsync();
        }
    }
}
