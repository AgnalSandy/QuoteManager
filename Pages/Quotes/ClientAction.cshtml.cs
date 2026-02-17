using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "Client")]
    public class ClientActionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientActionModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Quote Quote { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (quote == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || quote.ClientId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only respond to your own quotes.";
                return RedirectToPage("./Index");
            }

            if (quote.Status != QuoteStatus.Pending)
            {
                TempData["ErrorMessage"] = "This quote has already been responded to.";
                return RedirectToPage("./Details", new { id = quote.Id });
            }

            Quote = quote;
            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            return await ProcessResponse(id, QuoteStatus.Accepted);
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            return await ProcessResponse(id, QuoteStatus.Rejected);
        }

        private async Task<IActionResult> ProcessResponse(int id, string status)
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || quote.ClientId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only respond to your own quotes.";
                return RedirectToPage("./Index");
            }

            if (quote.Status != QuoteStatus.Pending)
            {
                TempData["ErrorMessage"] = "This quote has already been responded to.";
                return RedirectToPage("./Details", new { id = quote.Id });
            }

            // Update quote
            quote.Status = status;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = status == QuoteStatus.Accepted
                ? $"Quote '{quote.Title}' accepted successfully!"
                : $"Quote '{quote.Title}' rejected.";

            return RedirectToPage("./Index");
        }
    }
}
