using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Invoices
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<InvoiceListViewModel> Invoices { get; set; } = new List<InvoiceListViewModel>();
        public string CurrentUserRole { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            var roles = await _userManager.GetRolesAsync(currentUser);
            CurrentUserRole = roles.FirstOrDefault() ?? ApplicationRoles.Staff;

            // Build role-based query for invoices
            var invoicesQuery = await BuildInvoicesQueryAsync(currentUser);

            // Execute query and map to view models
            Invoices = await invoicesQuery
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new InvoiceListViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    ClientName = i.Client.FullName,
                    InvoiceDate = i.InvoiceDate,
                    GrandTotal = i.GrandTotal,
                    Status = i.Status,
                    TemplateType = i.TemplateType
                })
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation(
                "Loaded {Count} invoices for user {UserId} with role {Role}",
                Invoices.Count,
                currentUser.Id,
                CurrentUserRole);
        }

        private async Task<IQueryable<Invoice>> BuildInvoicesQueryAsync(ApplicationUser currentUser)
        {
            // Base query - include related data
            var invoicesQuery = _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Quote)
                .AsQueryable();

            // Apply role-based filtering
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                // SuperAdmin sees ALL invoices
                return invoicesQuery;
            }
            else if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Admin sees invoices for their clients and their staff's clients
                var accessibleClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser.Id || 
                                _context.Users.Any(staff => staff.Id == u.CreatedById && staff.CreatedById == currentUser.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                return invoicesQuery.Where(i => accessibleClientIds.Contains(i.ClientId));
            }
            else if (User.IsInRole(ApplicationRoles.Staff))
            {
                // Staff sees only invoices for clients they created
                var myClientIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                return invoicesQuery.Where(i => myClientIds.Contains(i.ClientId));
            }

            // Default: no invoices
            return invoicesQuery.Where(i => false);
        }
    }
}
