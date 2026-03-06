using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;

namespace QuoteManager.Pages.Api
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class GetServiceDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GetServiceDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int serviceId)
        {
            var service = await _context.ServiceMasters
                .Include(s => s.ServiceTaxes)
                    .ThenInclude(st => st.Tax)
                .FirstOrDefaultAsync(s => s.Id == serviceId);

            if (service == null)
            {
                return NotFound(new { error = "Service not found" });
            }

            var response = new
            {
                id = service.Id,
                serviceName = service.ServiceName,
                description = service.Description,
                unitPrice = service.ServiceCharge,
                taxes = service.ServiceTaxes.Select(st => new
                {
                    taxId = st.TaxId,
                    taxName = st.Tax.TaxName,
                    taxPercentage = st.Tax.TaxPercentage
                }).ToList()
            };

            return new JsonResult(response);
        }
    }
}
