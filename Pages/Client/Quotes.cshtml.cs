using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Pages.Client
{
    [Authorize(Roles = "Client")]
    public class QuotesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuotesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Quote> Quotes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public int AllCount { get; set; }
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Index");
            }

            // Get all my quotes for counts
            var allQuotes = await _context.Quotes
                .Where(q => q.ClientId == currentUser.Id)
                .ToListAsync();

            AllCount = allQuotes.Count;
            PendingCount = allQuotes.Count(q => q.Status == QuoteStatus.Pending);
            AcceptedCount = allQuotes.Count(q => q.Status == QuoteStatus.Accepted);
            RejectedCount = allQuotes.Count(q => q.Status == QuoteStatus.Rejected);

            // Get filtered quotes
            var quotesQuery = _context.Quotes
                .Where(q => q.ClientId == currentUser.Id)
                .Include(q => q.CreatedBy)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                quotesQuery = quotesQuery.Where(q => q.Status == StatusFilter);
            }

            Quotes = await quotesQuery
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            return Page();
        }
    }
}
