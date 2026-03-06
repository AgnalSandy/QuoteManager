using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.TaxMasters
{
    [Authorize(Roles = "SuperAdmin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TaxMasterViewModel>
    Taxes
        { get; set; } = new List<TaxMasterViewModel>
        ();

        public async Task OnGetAsync()
        {
            Taxes = await _context.TaxMasters
            .OrderBy(t => t.TaxName)
            .Select(t => new TaxMasterViewModel
            {
                Id = t.Id,
                TaxName = t.TaxName,
                TaxPercentage = t.TaxPercentage,
                Description = t.Description,
                IsActive = t.IsActive
            })
            .ToListAsync();
        }
    }
}
