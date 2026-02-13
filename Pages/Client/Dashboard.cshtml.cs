using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Client
{
    [Authorize(Roles = "Client")]
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // User properties
        public ApplicationUser CurrentClient { get; set; } = default!;
        public ApplicationUser? AssignedStaff { get; set; }
        public ApplicationUser? SupervisingAdmin { get; set; }
        public string WelcomeMessage { get; set; } = string.Empty;
        public bool HasAssignedStaff { get; set; }

        // Quote properties
        public List<Quote> MyQuotes { get; set; } = new List<Quote>();
        public int TotalQuotes { get; set; }
        public int PendingQuotes { get; set; }
        public int AcceptedQuotes { get; set; }
        public int RejectedQuotes { get; set; }
        public decimal TotalQuoteValue { get; set; }
        public decimal AcceptedValue { get; set; }
        public decimal PendingValue { get; set; }

        public async Task OnGetAsync()
        {
            // Get current client
            CurrentClient = await _userManager.GetUserAsync(User);

            if (CurrentClient == null)
            {
                WelcomeMessage = "Welcome, Guest!";
                return;
            }

            WelcomeMessage = $"Welcome, {CurrentClient.FullName}!";

            // Get assigned staff
            if (!string.IsNullOrEmpty(CurrentClient.CreatedById))
            {
                AssignedStaff = await _userManager.FindByIdAsync(CurrentClient.CreatedById);
                HasAssignedStaff = AssignedStaff != null;

                // Get supervising admin
                if (AssignedStaff != null && !string.IsNullOrEmpty(AssignedStaff.CreatedById))
                {
                    SupervisingAdmin = await _userManager.FindByIdAsync(AssignedStaff.CreatedById);
                }
            }

            // Get MY quotes
            MyQuotes = await _context.Quotes
                .Where(q => q.ClientId == CurrentClient.Id)
                .OrderByDescending(q => q.CreatedDate)
                .Include(q => q.CreatedBy)
                .Take(5)  // Show latest 5 quotes
                .ToListAsync();

            // Calculate quote statistics
            var allMyQuotes = await _context.Quotes
                .Where(q => q.ClientId == CurrentClient.Id)
                .ToListAsync();

            TotalQuotes = allMyQuotes.Count;
            PendingQuotes = allMyQuotes.Count(q => q.Status == QuoteStatus.Pending);
            AcceptedQuotes = allMyQuotes.Count(q => q.Status == QuoteStatus.Accepted);
            RejectedQuotes = allMyQuotes.Count(q => q.Status == QuoteStatus.Rejected);

            TotalQuoteValue = allMyQuotes.Sum(q => q.Amount);
            AcceptedValue = allMyQuotes.Where(q => q.Status == QuoteStatus.Accepted).Sum(q => q.Amount);
            PendingValue = allMyQuotes.Where(q => q.Status == QuoteStatus.Pending).Sum(q => q.Amount);
        }
    }
}
