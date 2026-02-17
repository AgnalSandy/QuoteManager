using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff,Client")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Quote Quote { get; set; } = default!;
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool IsClient { get; set; }

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
            if (currentUser == null)
            {
                return Forbid();
            }

            // Verify access
            var hasAccess = await VerifyQuoteAccess(currentUser.Id, quote);
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this quote.";
                return RedirectToPage("./Index");
            }

            Quote = quote;
            IsClient = User.IsInRole("Client");
            CanEdit = !IsClient;
            CanDelete = !IsClient;

            return Page();
        }

        private async Task<bool> VerifyQuoteAccess(string userId, Quote quote)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            if (User.IsInRole("Admin"))
            {
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
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == quote.ClientId && u.CreatedById == userId);

                return isMyClient;
            }

            if (User.IsInRole("Client"))
            {
                return quote.ClientId == userId;
            }

            return false;
        }
    }
}