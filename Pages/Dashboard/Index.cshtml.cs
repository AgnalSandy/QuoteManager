using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            // User counts (excluding self)
            var allUsers = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .ToListAsync();

            TotalUsersCount = allUsers.Count;

            foreach (var user in allUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var role = userRoles.FirstOrDefault();

                if (role == "Admin") AdminCount++;
                else if (role == "Staff") StaffCount++;
                else if (role == "Client") ClientCount++;
            }

            // Quote & Revenue stats (ALL quotes)
            var allQuotes = await _context.Quotes.ToListAsync();

            TotalQuotes = allQuotes.Count;
            PendingQuotes = allQuotes.Count(q => q.Status == QuoteStatus.Pending);
            AcceptedQuotes = allQuotes.Count(q => q.Status == QuoteStatus.Accepted);  // ⭐ Use your status
            RejectedQuotes = allQuotes.Count(q => q.Status == QuoteStatus.Rejected);

            TotalRevenue = allQuotes.Sum(q => q.Amount);
            PendingRevenue = allQuotes.Where(q => q.Status == QuoteStatus.Pending).Sum(q => q.Amount);
            AcceptedRevenue = allQuotes.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => q.Amount);  // ⭐
            RejectedRevenue = allQuotes.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => q.Amount);
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;
        }

        // ==================== Admin Stats ====================
        private async Task CalculateAdminStats(string adminId)
        {
            // Get MY staff
            var myStaff = await _userManager.Users
                .Where(u => u.CreatedById == adminId)
                .ToListAsync();

            var myStaffUsers = new List<ApplicationUser>();
            foreach (var user in myStaff)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Staff"))
                {
                    myStaffUsers.Add(user);
                }
            }

            StaffCount = myStaffUsers.Count;
            var myStaffIds = myStaffUsers.Select(s => s.Id).ToList();

            // Get clients of MY staff
            var myStaffClients = await _userManager.Users
                .Where(u => myStaffIds.Contains(u.CreatedById))
                .ToListAsync();

            ClientCount = 0;
            var myClientIds = new List<string>();
            foreach (var user in myStaffClients)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Client"))
                {
                    ClientCount++;
                    myClientIds.Add(user.Id);
                }
            }

            TotalUsersCount = StaffCount + ClientCount;
            AdminCount = 0;

            // Get quotes for MY team's clients
            var myTeamQuotes = await _context.Quotes
                .Where(q => myClientIds.Contains(q.ClientId))
                .ToListAsync();

            TotalQuotes = myTeamQuotes.Count;
            PendingQuotes = myTeamQuotes.Count(q => q.Status == QuoteStatus.Pending);
            AcceptedQuotes = myTeamQuotes.Count(q => q.Status == QuoteStatus.Accepted);
            RejectedQuotes = myTeamQuotes.Count(q => q.Status == QuoteStatus.Rejected);

            TotalRevenue = myTeamQuotes.Sum(q => q.Amount);
            PendingRevenue = myTeamQuotes.Where(q => q.Status == QuoteStatus.Pending).Sum(q => q.Amount);
            AcceptedRevenue = myTeamQuotes.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => q.Amount);
            RejectedRevenue = myTeamQuotes.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => q.Amount);
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;
        }

        // ==================== Staff Stats ====================
        private async Task CalculateStaffStats(string staffId)
        {
            // Get MY clients
            var myClients = await _userManager.Users
                .Where(u => u.CreatedById == staffId)
                .ToListAsync();

            ClientCount = 0;
            var myClientIds = new List<string>();
            foreach (var user in myClients)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Client"))
                {
                    ClientCount++;
                    myClientIds.Add(user.Id);
                }
            }

            TotalUsersCount = ClientCount;
            AdminCount = 0;
            StaffCount = 0;

            // Get quotes for MY clients
            var myClientQuotes = await _context.Quotes
                .Where(q => myClientIds.Contains(q.ClientId))
                .ToListAsync();

            TotalQuotes = myClientQuotes.Count;
            PendingQuotes = myClientQuotes.Count(q => q.Status == QuoteStatus.Pending);
            AcceptedQuotes = myClientQuotes.Count(q => q.Status == QuoteStatus.Accepted);
            RejectedQuotes = myClientQuotes.Count(q => q.Status == QuoteStatus.Rejected);

            TotalRevenue = myClientQuotes.Sum(q => q.Amount);
            PendingRevenue = myClientQuotes.Where(q => q.Status == QuoteStatus.Pending).Sum(q => q.Amount);
            AcceptedRevenue = myClientQuotes.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => q.Amount);
            RejectedRevenue = myClientQuotes.Where(q => q.Status == QuoteStatus.Rejected).Sum(q => q.Amount);
            AverageQuoteValue = TotalQuotes > 0 ? TotalRevenue / TotalQuotes : 0;
        }
    }
}
