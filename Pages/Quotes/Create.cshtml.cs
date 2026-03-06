using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Constants;
using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace QuoteManager.Pages.Quotes
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IQuoteNumberGenerator _quoteNumberGenerator;
        private readonly IMasterDataService _masterDataService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IQuoteNumberGenerator quoteNumberGenerator,
            IMasterDataService masterDataService,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _quoteNumberGenerator = quoteNumberGenerator;
            _masterDataService = masterDataService;
            _logger = logger;
        }

        [BindProperty]
        public CreateQuoteViewModel Input { get; set; } = new CreateQuoteViewModel();

        public Dictionary<int, decimal> ServicePrices { get; set; } = new();
        public Dictionary<int, decimal> TaxRates { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdowns();
            
            // Items will be added by JavaScript dynamically
            Input.Items = new List<QuoteItemViewModel>();
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove validation errors for collections if needed
            ModelState.Remove("Input.AvailableClients");
            ModelState.Remove("Input.AvailableServices");
            ModelState.Remove("Input.AvailableTaxes");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            // Validate at least one item
            if (Input.Items == null || !Input.Items.Any())
            {
                ModelState.AddModelError("", "Please add at least one quote item.");
                await LoadDropdowns();
                return Page();
            }

            // Validate each item
            try
            {
                foreach (var item in Input.Items)
                {
                    item.Validate(); // Throws ValidationException if invalid
                }
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadDropdowns();
                return Page();
            }

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                await LoadDropdowns();
                return Page();
            }

            // Verify user has permission to create quote for this client
            var isValidClient = await VerifyClientAccess(currentUser.Id, Input.ClientId);
            if (!isValidClient)
            {
                ModelState.AddModelError("Input.ClientId", "You don't have permission to create quotes for this client.");
                await LoadDropdowns();
                return Page();
            }

            // Use transaction for data integrity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Calculate totals
                decimal subTotal = 0;
                decimal totalTax = 0;

                // Generate quote title if not provided
                string quoteTitle = Input.Title;
                if (string.IsNullOrWhiteSpace(quoteTitle))
                {
                    quoteTitle = await GenerateQuoteTitle(Input.Items, Input.ClientId);
                }

                // Create the quote with thread-safe number generation
                var quote = new Quote
                {
                    QuoteNumber = await _quoteNumberGenerator.GenerateAsync(),
                    Title = quoteTitle,
                    Description = Input.Description,
                    ClientId = Input.ClientId,
                    CreatedById = currentUser.Id,
                    CreatedDate = DateTime.UtcNow,
                    ValidUntil = Input.ValidUntil,
                    Status = QuoteStatus.Pending
                };

                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync(); // Save to get the QuoteId

                // Add quote items in batch
                var quoteItems = new List<QuoteItem>();
                var quoteItemTaxes = new List<QuoteItemTax>();

                foreach (var itemInput in Input.Items)
                {
                    var quoteItem = new QuoteItem
                    {
                        QuoteId = quote.Id,
                        ServiceId = itemInput.IsCustomService ? null : itemInput.ServiceId,
                        CustomServiceName = itemInput.CustomServiceName,
                        Quantity = itemInput.Quantity,
                        UnitPrice = itemInput.UnitPrice,
                        Amount = itemInput.Quantity * itemInput.UnitPrice,
                        CustomDescription = itemInput.CustomDescription
                    };

                    quoteItems.Add(quoteItem);
                    subTotal += quoteItem.Amount;
                }

                // Save all items at once
                _context.QuoteItems.AddRange(quoteItems);
                await _context.SaveChangesAsync(); // Get QuoteItemIds

                // Add taxes
                for (int i = 0; i < quoteItems.Count; i++)
                {
                    var itemInput = Input.Items[i];
                    var quoteItem = quoteItems[i];

                    if (itemInput.SelectedTaxIds != null && itemInput.SelectedTaxIds.Any())
                    {
                        foreach (var taxId in itemInput.SelectedTaxIds)
                        {
                            var tax = await _masterDataService.GetTaxByIdAsync(taxId);
                            if (tax != null)
                            {
                                var taxAmount = (quoteItem.Amount * tax.TaxPercentage) / 100;
                                quoteItemTaxes.Add(new QuoteItemTax
                                {
                                    QuoteItemId = quoteItem.Id,
                                    TaxId = taxId,
                                    TaxAmount = taxAmount
                                });
                                totalTax += taxAmount;
                            }
                        }
                    }
                }

                // Save all taxes at once
                if (quoteItemTaxes.Any())
                {
                    _context.QuoteItemTaxes.AddRange(quoteItemTaxes);
                }

                // Update quote totals
                quote.SubTotal = subTotal;
                quote.TotalTax = totalTax;
                quote.GrandTotal = subTotal + totalTax;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Quote {QuoteNumber} created by user {UserId} for client {ClientId}",
                    quote.QuoteNumber,
                    currentUser.Id,
                    Input.ClientId);

                TempData[TempDataKeys.Success] = $"Quote '{quote.QuoteNumber}' created successfully!";
                return RedirectToPage("./Details", new { id = quote.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating quote");
                ModelState.AddModelError("", "An error occurred while creating the quote. Please try again.");
                await LoadDropdowns();
                return Page();
            }
        }

        private async Task<string> GenerateQuoteTitle(List<QuoteItemViewModel> items, string clientId)
        {
            var serviceNames = new List<string>();
            
            foreach (var item in items.Take(3))
            {
                if (item.IsCustomService && !string.IsNullOrWhiteSpace(item.CustomServiceName))
                {
                    serviceNames.Add(item.CustomServiceName);
                }
                else if (item.ServiceId.HasValue)
                {
                    var service = await _masterDataService.GetServiceByIdAsync(item.ServiceId.Value);
                    if (service != null)
                    {
                        serviceNames.Add(service.ServiceName);
                    }
                }
            }

            string title;
            if (serviceNames.Any())
            {
                title = string.Join(", ", serviceNames);
                if (items.Count > 3)
                {
                    title += $" +{items.Count - 3} more";
                }
            }
            else
            {
                var client = await _userManager.FindByIdAsync(clientId);
                title = $"Quote for {client?.FullName ?? "Client"}";
            }

            // Limit title length
            if (title.Length > 200)
            {
                title = title.Substring(0, 197) + "...";
            }

            return title;
        }

        private async Task LoadDropdowns()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            // Load Clients - OPTIMIZED to reduce N+1 queries
            var clients = new List<ApplicationUser>();

            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                // SuperAdmin sees ALL clients - single query with join
                clients = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                    .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.User, r.Name })
                    .Where(x => x.Name == ApplicationRoles.Client)
                    .Select(x => x.User)
                    .ToListAsync();
            }
            else if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Admin sees their team's clients - optimized query
                var myStaffIds = await _context.Users
                    .Where(u => u.CreatedById == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                clients = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                    .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.User, r.Name })
                    .Where(x => x.Name == ApplicationRoles.Client && myStaffIds.Contains(x.User.CreatedById))
                    .Select(x => x.User)
                    .ToListAsync();
            }
            else if (User.IsInRole(ApplicationRoles.Staff))
            {
                // Staff sees their own clients - optimized query
                clients = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                    .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.User, r.Name })
                    .Where(x => x.Name == ApplicationRoles.Client && x.User.CreatedById == currentUser.Id)
                    .Select(x => x.User)
                    .ToListAsync();
            }

            Input.AvailableClients = clients
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = c.FullName
                }).ToList();

            // Load Services from cache with prices
            var services = await _masterDataService.GetActiveServicesAsync();

            Input.AvailableServices = services.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.ServiceName} - ₹{s.ServiceCharge:N2}"
            }).ToList();

            ServicePrices = services.ToDictionary(s => s.Id, s => s.ServiceCharge);

            // Load Taxes from cache with rates
            var taxes = await _masterDataService.GetActiveTaxesAsync();

            Input.AvailableTaxes = taxes.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.TaxName} ({t.TaxPercentage}%)"
            }).ToList();

            TaxRates = taxes.ToDictionary(t => t.Id, t => t.TaxPercentage);
        }

        private async Task<bool> VerifyClientAccess(string userId, string clientId)
        {
            if (User.IsInRole(ApplicationRoles.SuperAdmin))
            {
                return true; // SuperAdmin has access to all clients
            }

            if (User.IsInRole(ApplicationRoles.Admin))
            {
                // Check if client belongs to admin's team
                var isMyTeamClient = await _context.Users
                    .AnyAsync(u => u.Id == clientId && 
                                  _context.Users.Any(staff => staff.Id == u.CreatedById && staff.CreatedById == userId));

                return isMyTeamClient;
            }

            if (User.IsInRole(ApplicationRoles.Staff))
            {
                // Check if client belongs to staff member
                var isMyClient = await _context.Users
                    .AnyAsync(u => u.Id == clientId && u.CreatedById == userId);

                return isMyClient;
            }

            return false;
        }
    }
}
