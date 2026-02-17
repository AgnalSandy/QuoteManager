using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Quote Quote { get; set; } = default!;

        public SelectList ClientsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadClientsDropdown();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadClientsDropdown();
                return Page();
            }

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                await LoadClientsDropdown();
                return Page();
            }

            // Validate client selection
            if (string.IsNullOrEmpty(Quote.ClientId))
            {
                ModelState.AddModelError("Quote.ClientId", "Please select a client.");
                await LoadClientsDropdown();
                return Page();
            }

            // Verify user has permission to create quote for this client
            var isValidClient = await VerifyClientAccess(currentUser.Id, Quote.ClientId);
            if (!isValidClient)
            {
                ModelState.AddModelError("Quote.ClientId", "You don't have permission to create quotes for this client.");
                await LoadClientsDropdown();
                return Page();
            }

            // Generate unique QuoteNumber
            Quote.QuoteNumber = await GenerateQuoteNumber();

            // Set automatic fields
            Quote.CreatedById = currentUser.Id;
            Quote.CreatedDate = DateTime.UtcNow;
            Quote.Status = QuoteStatus.Pending;

            // Add to database
            _context.Quotes.Add(Quote);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Quote '{Quote.Title}' (#{Quote.QuoteNumber}) created successfully!";
            return RedirectToPage("./Index");
        }

        private async Task<string> GenerateQuoteNumber()
        {
            // Format: Q-YYYY-####
            var year = DateTime.UtcNow.Year;
            var lastQuote = await _context.Quotes
                .Where(q => q.QuoteNumber.StartsWith($"Q-{year}-"))
                .OrderByDescending(q => q.QuoteNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastQuote != null)
            {
                var lastNumberStr = lastQuote.QuoteNumber.Split('-').Last();
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"Q-{year}-{nextNumber:D4}"; // D4 = 4 digits with leading zeros
        }

        private async Task LoadClientsDropdown()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            var clientsQuery = _userManager.Users.AsQueryable();

            if (User.IsInRole("SuperAdmin"))
            {
                // SuperAdmin sees ALL clients
                var allUsers = await _userManager.Users.ToListAsync();
                var clients = new List<ApplicationUser>();

                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Client"))
                    {
                        clients.Add(user);
                    }
                }

                ClientsList = new SelectList(clients, "Id", "FullName");
            }
            else if (User.IsInRole("Admin"))
            {
                // Admin sees their team's clients
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                var teamClients = await _userManager.Users
                    .Where(u => myStaffIds.Contains(u.CreatedById))
                    .ToListAsync();

                var clients = new List<ApplicationUser>();
                foreach (var user in teamClients)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Client"))
                    {
                        clients.Add(user);
                    }
                }

                ClientsList = new SelectList(clients, "Id", "FullName");
            }
            else if (User.IsInRole("Staff"))
            {
                // Staff sees their own clients
                var myClients = await _userManager.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .ToListAsync();

                var clients = new List<ApplicationUser>();
                foreach (var user in myClients)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Client"))
                    {
                        clients.Add(user);
                    }
                }

                ClientsList = new SelectList(clients, "Id", "FullName");
            }
        }

        private async Task<bool> VerifyClientAccess(string userId, string clientId)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return true; // SuperAdmin has access to all clients
            }

            if (User.IsInRole("Admin"))
            {
                // Check if client belongs to admin's team
                var myStaffIds = await _userManager.Users
                    .Where(u => u.CreatedById == userId)
                    .Select(u => u.Id)
                    .ToListAsync();

                var isMyTeamClient = await _userManager.Users
                    .AnyAsync(u => u.Id == clientId && myStaffIds.Contains(u.CreatedById));

                return isMyTeamClient;
            }

            if (User.IsInRole("Staff"))
            {
                // Check if client belongs to staff member
                var isMyClient = await _userManager.Users
                    .AnyAsync(u => u.Id == clientId && u.CreatedById == userId);

                return isMyClient;
            }

            return false;
        }
    }
}
