using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff,Client")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        private const int PageSize = 20;

        public IndexModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<Quote> Quotes { get; set; } = new List<Quote>();
        public string CurrentUserRole { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            var roles = await _userManager.GetRolesAsync(currentUser);
            CurrentUserRole = roles.FirstOrDefault() ?? ApplicationRoles.Client;

            // Build optimized query with single pass
            var quotesQuery = await BuildQuotesQueryAsync(currentUser);

            // Apply filters
            quotesQuery = ApplyFilters(quotesQuery);

            // Get total count for pagination
            TotalCount = await quotesQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Ensure page number is valid
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Execute query with pagination - optimized with AsNoTracking
            Quotes = await quotesQuery
                .OrderByDescending(q => q.CreatedDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .AsNoTracking() // Performance optimization - read-only
                .ToListAsync();

            _logger.LogInformation(
                "Loaded {Count} quotes for user {UserId} (Page {Page} of {TotalPages})",
                Quotes.Count,
                currentUser.Id,
                PageNumber,
                TotalPages);
        }

        private async Task<IQueryable<Quote>> BuildQuotesQueryAsync(ApplicationUser currentUser)
        {
            // Base query - include related data (prevents N+1)
            var quotesQuery = _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.CreatedBy)
                .AsQueryable();

            // Apply role-based filtering with optimized queries
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                // SuperAdmin sees ALL quotes - no filtering needed
                return quotesQuery;
            }
            else if (User.IsInRole(ApplicationRoles.Admin))
            {
                // OPTIMIZED: Single query to get all accessible client IDs
                var accessibleClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser.Id || 
                                _context.Users.Any(staff => staff.Id == u.CreatedById && staff.CreatedById == currentUser.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                return quotesQuery.Where(q => accessibleClientIds.Contains(q.ClientId));
            }
            else if (User.IsInRole(ApplicationRoles.Staff))
            {
                // OPTIMIZED: Single query to get staff's client IDs
                var myClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                return quotesQuery.Where(q => myClientIds.Contains(q.ClientId));
            }
            else if (User.IsInRole(ApplicationRoles.Client))
            {
                // Client sees only their own quotes
                return quotesQuery.Where(q => q.ClientId == currentUser.Id);
            }

            // Default: no quotes
            return quotesQuery.Where(q => false);
        }

        private IQueryable<Quote> ApplyFilters(IQueryable<Quote> query)
        {
            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All" && QuoteStatus.IsValid(StatusFilter))
            {
                query = query.Where(q => q.Status == StatusFilter);
            }

            // Apply search term
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(q =>
                    q.QuoteNumber.ToLower().Contains(searchLower) ||
                    q.Title.ToLower().Contains(searchLower) ||
                    (q.Description != null && q.Description.ToLower().Contains(searchLower)) ||
                    q.Client.FullName.ToLower().Contains(searchLower)
                );
            }

            return query;
        }
    }
}