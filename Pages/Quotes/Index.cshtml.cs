using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff,Client")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Quote> Quotes { get; set; } = new List<Quote>();
        public string CurrentUserRole { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            var roles = await _userManager.GetRolesAsync(currentUser);
            CurrentUserRole = roles.FirstOrDefault() ?? "User";

            // Base query - include related data
            var quotesQuery = _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.CreatedBy)
                .AsQueryable();

            // Apply role-based filtering
            if (User.IsInRole("SuperAdmin"))
            {
                // SuperAdmin sees ALL quotes
                // No filtering needed
            }
            else if (User.IsInRole("Admin"))
            {
                // Admin sees quotes for their team's clients
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                var myTeamClientIds = await _userManager.Users
                    .Where(u => myStaffIds.Contains(u.CreatedById))
                    .Select(u => u.Id)
                    .ToListAsync();

                quotesQuery = quotesQuery.Where(q => myTeamClientIds.Contains(q.ClientId));
            }
            else if (User.IsInRole("Staff"))
            {
                // Staff sees quotes for their own clients
                var myClientIds = await _userManager.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                quotesQuery = quotesQuery.Where(q => myClientIds.Contains(q.ClientId));
            }
            else if (User.IsInRole("Client"))
            {
                // Client sees only their own quotes
                quotesQuery = quotesQuery.Where(q => q.ClientId == currentUser.Id);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                quotesQuery = quotesQuery.Where(q => q.Status == StatusFilter);
            }

            // Apply search term
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                quotesQuery = quotesQuery.Where(q =>
                    q.Title.Contains(SearchTerm) ||
                    q.Description!.Contains(SearchTerm) ||
                    q.Client.FullName.Contains(SearchTerm)
                );
            }

            // Execute query and order by date
            Quotes = await quotesQuery
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();
        }
    }
}