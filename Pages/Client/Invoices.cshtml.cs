using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Pages.Client
{
    [Authorize(Roles = "Client")]
    public class InvoicesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InvoicesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Invoice> Invoices { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Index");
            }

            Invoices = await _context.Invoices
                .Where(i => i.ClientId == currentUser.Id)
                .Include(i => i.Quote)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return Page();
        }
    }
}
