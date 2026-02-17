using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
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
        public Quote Quote { get; set; } = default!;

        public SelectList StatusList { get; set; } = default!;
        public string ClientName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;

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

            // Verify user has permission to edit this quote
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            var hasAccess = await VerifyQuoteAccess(currentUser.Id, quote);
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this quote.";
                return RedirectToPage("./Index");
            }

            Quote = quote;
            ClientName = quote.Client.FullName;
            CreatedByName = quote.CreatedBy.FullName;

            LoadStatusList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadStatusList();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            // Get original quote from database
            var quoteToUpdate = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.CreatedBy)
                .FirstOrDefaultAsync(q => q.Id == Quote.Id);

            if (quoteToUpdate == null)
            {
                return NotFound();
            }

            // Verify access
            var hasAccess = await VerifyQuoteAccess(currentUser.Id, quoteToUpdate);
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this quote.";
                return RedirectToPage("./Index");
            }

            // Update allowed fields only
            quoteToUpdate.Title = Quote.Title;
            quoteToUpdate.Description = Quote.Description;
            quoteToUpdate.Amount = Quote.Amount;
            quoteToUpdate.Status = Quote.Status;
            quoteToUpdate.ValidUntil = Quote.ValidUntil;

            // Do NOT update: QuoteNumber, ClientId, CreatedById, CreatedDate

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Quote '{quoteToUpdate.Title}' updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuoteExists(Quote.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private void LoadStatusList()
        {
            StatusList = new SelectList(new[]
            {
                QuoteStatus.Pending,
                QuoteStatus.Accepted,
                QuoteStatus.Rejected,
                QuoteStatus.Expired
            });
        }

        private async Task<bool> VerifyQuoteAccess(string userId, Quote quote)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true; // SuperAdmin has access to all
            }

            if (User.IsInRole("Admin"))
            {
                // Check if quote belongs to admin's team's clients
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id)
                    .ToListAsync();

                var isMyTeamClient = await _userManager.Users
                    .AnyAsync(u => u.Id == quote.ClientId && myStaffIds.Contains(u.CreatedById));

                return isMyTeamClient;
            }

            if (User.IsInRole("Staff"))
            {
                // Check if quote belongs to staff's client
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == quote.ClientId && u.CreatedById == userId);

                return isMyClient;
            }

            return false;
        }

        private bool QuoteExists(int id)
        {
            return _context.Quotes.Any(e => e.Id == id);
        }
    }
}
