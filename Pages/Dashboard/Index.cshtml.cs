using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Data;
using QuoteManager.Models;
using System.Threading.Tasks;
using System.Linq;

namespace QuoteManager.Pages.Dashboard
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // User properties
        public string CurrentUserRole { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string WelcomeMessage { get; set; } = string.Empty;
        public string CountDescription { get; set; } = string.Empty;

        // User counts
        public int TotalUsersCount { get; set; }
        public int AdminCount { get; set; }
        public int StaffCount { get; set; }
        public int ClientCount { get; set; }

        // Quote counts
        public int TotalQuotes { get; set; }
        public int PendingQuotes { get; set; }
        public int AcceptedQuotes { get; set; }  // ⭐ Use "Accepted" if your model uses this
        public int RejectedQuotes { get; set; }

        // Revenue stats
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal AcceptedRevenue { get; set; }  // ⭐ Use "Accepted" if your model uses this
        public decimal RejectedRevenue { get; set; }
        public decimal AverageQuoteValue { get; set; }

        // Invoice stats
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalInvoiceRevenue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                WelcomeMessage = "Welcome, Guest!";
                return;
            }

            CurrentUserName = currentUser.FullName ?? "User";
            var roles = await _userManager.GetRolesAsync(currentUser);
            CurrentUserRole = roles.FirstOrDefault() ?? "User";

            // Calculate stats based on role
            if (User.IsInRole("SuperAdmin"))
            {
                await CalculateSuperAdminStats(currentUser.Id);
                WelcomeMessage = $"Welcome, {CurrentUserName}!";
                CountDescription = "Complete System Overview";
            }
            else if (User.IsInRole("Admin"))
            {
                await CalculateAdminStats(currentUser.Id);
                WelcomeMessage = $"Welcome, {CurrentUserName}!";
                CountDescription = "Your Team Performance";
            }
            else if (User.IsInRole("Staff"))
            {
                await CalculateStaffStats(currentUser.Id);
                WelcomeMessage = $"Welcome, {CurrentUserName}!";
                CountDescription = "Your Client Statistics";
            }
        }

        // ==================== SuperAdmin Stats ====================
        private async Task CalculateSuperAdminStats(string currentUserId)
        {
            // OPTIMIZED: User counts (excluding self) - single query
            TotalUsersCount = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .CountAsync();

            // Get role counts efficiently
            var allUsers = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => u.Id)
                .ToListAsync();

            AdminCount = 0;
            StaffCount = 0;
            ClientCount = 0;

            foreach (var userId in allUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault();

                    if (role == "Admin") AdminCount++;
                    else if (role == "Staff") StaffCount++;
                    else if (role == "Client") ClientCount++;
                }
            }

            // OPTIMIZED: Quote & Revenue stats - database aggregation
            var quoteStats = await _context.Quotes
                .GroupBy(q => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PendingCount = g.Count(q => q.Status == QuoteStatus.Pending),
                    AcceptedCount = g.Count(q => q.Status == QuoteStatus.Accepted),
                    RejectedCount = g.Count(q => q.Status == QuoteStatus.Rejected),
                    TotalRev = g.Sum(q => q.GrandTotal),
                    PendingRev = g.Where(q => q.Status == QuoteStatus.Pending).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    AcceptedRev = g.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    RejectedRev = g.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => (decimal?)q.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalQuotes = quoteStats?.TotalCount ?? 0;
            PendingQuotes = quoteStats?.PendingCount ?? 0;
            AcceptedQuotes = quoteStats?.AcceptedCount ?? 0;
            RejectedQuotes = quoteStats?.RejectedCount ?? 0;
            TotalRevenue = quoteStats?.TotalRev ?? 0;
            PendingRevenue = quoteStats?.PendingRev ?? 0;
            AcceptedRevenue = quoteStats?.AcceptedRev ?? 0;
            RejectedRevenue = quoteStats?.RejectedRev ?? 0;
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;

            // OPTIMIZED: Invoice stats - database aggregation
            var invoiceStats = await _context.Invoices
                .GroupBy(i => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PaidCount = g.Count(i => i.Status == InvoiceStatus.Paid),
                    UnpaidCount = g.Count(i => i.Status != InvoiceStatus.Paid),
                    TotalRev = g.Sum(i => i.GrandTotal),
                    PaidAmt = g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0,
                    UnpaidAmt = g.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalInvoices = invoiceStats?.TotalCount ?? 0;
            PaidInvoices = invoiceStats?.PaidCount ?? 0;
            UnpaidInvoices = invoiceStats?.UnpaidCount ?? 0;
            TotalInvoiceRevenue = invoiceStats?.TotalRev ?? 0;
            PaidAmount = invoiceStats?.PaidAmt ?? 0;
            UnpaidAmount = invoiceStats?.UnpaidAmt ?? 0;
        }

        // ==================== Admin Stats ====================
        private async Task CalculateAdminStats(string adminId)
        {
            // OPTIMIZED: Get MY staff IDs in single query
            var myStaffIds = await _userManager.Users
                .Where(u => u.CreatedById == adminId)
                .Select(u => u.Id)
                .ToListAsync();

            // Count staff efficiently
            StaffCount = 0;
            foreach (var staffId in myStaffIds)
            {
                var user = await _userManager.FindByIdAsync(staffId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Staff"))
                    {
                        StaffCount++;
                    }
                }
            }

            // OPTIMIZED: Get clients of MY staff in single query
            var myClientIds = await _userManager.Users
                .Where(u => myStaffIds.Contains(u.CreatedById))
                .Select(u => u.Id)
                .ToListAsync();

            // Count clients efficiently
            ClientCount = 0;
            var validClientIds = new List<string>();
            foreach (var clientId in myClientIds)
            {
                var user = await _userManager.FindByIdAsync(clientId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Client"))
                    {
                        ClientCount++;
                        validClientIds.Add(clientId);
                    }
                }
            }

            TotalUsersCount = StaffCount + ClientCount;
            AdminCount = 0;

            // OPTIMIZED: Quote stats - database aggregation
            var quoteStats = await _context.Quotes
                .Where(q => validClientIds.Contains(q.ClientId))
                .GroupBy(q => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PendingCount = g.Count(q => q.Status == QuoteStatus.Pending),
                    AcceptedCount = g.Count(q => q.Status == QuoteStatus.Accepted),
                    RejectedCount = g.Count(q => q.Status == QuoteStatus.Rejected),
                    TotalRev = g.Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    PendingRev = g.Where(q => q.Status == QuoteStatus.Pending).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    AcceptedRev = g.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    RejectedRev = g.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => (decimal?)q.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalQuotes = quoteStats?.TotalCount ?? 0;
            PendingQuotes = quoteStats?.PendingCount ?? 0;
            AcceptedQuotes = quoteStats?.AcceptedCount ?? 0;
            RejectedQuotes = quoteStats?.RejectedCount ?? 0;
            TotalRevenue = quoteStats?.TotalRev ?? 0;
            PendingRevenue = quoteStats?.PendingRev ?? 0;
            AcceptedRevenue = quoteStats?.AcceptedRev ?? 0;
            RejectedRevenue = quoteStats?.RejectedRev ?? 0;
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;

            // OPTIMIZED: Invoice stats - database aggregation
            var invoiceStats = await _context.Invoices
                .Where(i => validClientIds.Contains(i.ClientId))
                .GroupBy(i => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PaidCount = g.Count(i => i.Status == InvoiceStatus.Paid),
                    UnpaidCount = g.Count(i => i.Status != InvoiceStatus.Paid),
                    TotalRev = g.Sum(i => (decimal?)i.GrandTotal) ?? 0,
                    PaidAmt = g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0,
                    UnpaidAmt = g.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalInvoices = invoiceStats?.TotalCount ?? 0;
            PaidInvoices = invoiceStats?.PaidCount ?? 0;
            UnpaidInvoices = invoiceStats?.UnpaidCount ?? 0;
            TotalInvoiceRevenue = invoiceStats?.TotalRev ?? 0;
            PaidAmount = invoiceStats?.PaidAmt ?? 0;
            UnpaidAmount = invoiceStats?.UnpaidAmt ?? 0;
        }

        // ==================== Staff Stats ====================
        private async Task CalculateStaffStats(string staffId)
        {
            // OPTIMIZED: Get MY client IDs in single query
            var myClientIds = await _userManager.Users
                .Where(u => u.CreatedById == staffId)
                .Select(u => u.Id)
                .ToListAsync();

            // Count clients efficiently
            ClientCount = 0;
            var validClientIds = new List<string>();
            foreach (var clientId in myClientIds)
            {
                var user = await _userManager.FindByIdAsync(clientId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Client"))
                    {
                        ClientCount++;
                        validClientIds.Add(clientId);
                    }
                }
            }

            TotalUsersCount = ClientCount;
            AdminCount = 0;
            StaffCount = 0;

            // OPTIMIZED: Quote stats - database aggregation
            var quoteStats = await _context.Quotes
                .Where(q => validClientIds.Contains(q.ClientId))
                .GroupBy(q => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PendingCount = g.Count(q => q.Status == QuoteStatus.Pending),
                    AcceptedCount = g.Count(q => q.Status == QuoteStatus.Accepted),
                    RejectedCount = g.Count(q => q.Status == QuoteStatus.Rejected),
                    TotalRev = g.Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    PendingRev = g.Where(q => q.Status == QuoteStatus.Pending).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    AcceptedRev = g.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => (decimal?)q.GrandTotal) ?? 0,
                    RejectedRev = g.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => (decimal?)q.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalQuotes = quoteStats?.TotalCount ?? 0;
            PendingQuotes = quoteStats?.PendingCount ?? 0;
            AcceptedQuotes = quoteStats?.AcceptedCount ?? 0;
            RejectedQuotes = quoteStats?.RejectedCount ?? 0;
            TotalRevenue = quoteStats?.TotalRev ?? 0;
            PendingRevenue = quoteStats?.PendingRev ?? 0;
            AcceptedRevenue = quoteStats?.AcceptedRev ?? 0;
            RejectedRevenue = quoteStats?.RejectedRev ?? 0;
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;

            // OPTIMIZED: Invoice stats - database aggregation
            var invoiceStats = await _context.Invoices
                .Where(i => validClientIds.Contains(i.ClientId))
                .GroupBy(i => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PaidCount = g.Count(i => i.Status == InvoiceStatus.Paid),
                    UnpaidCount = g.Count(i => i.Status != InvoiceStatus.Paid),
                    TotalRev = g.Sum(i => (decimal?)i.GrandTotal) ?? 0,
                    PaidAmt = g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0,
                    UnpaidAmt = g.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => (decimal?)i.GrandTotal) ?? 0
                })
                .FirstOrDefaultAsync();

            TotalInvoices = invoiceStats?.TotalCount ?? 0;
            PaidInvoices = invoiceStats?.PaidCount ?? 0;
            UnpaidInvoices = invoiceStats?.UnpaidCount ?? 0;
            TotalInvoiceRevenue = invoiceStats?.TotalRev ?? 0;
            PaidAmount = invoiceStats?.PaidAmt ?? 0;
            UnpaidAmount = invoiceStats?.UnpaidAmt ?? 0;
        }
    }
}
