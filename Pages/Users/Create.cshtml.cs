using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuoteManager.Constants;
using QuoteManager.Models;
using QuoteManager.ViewModels;

namespace QuoteManager.Pages.Users
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(UserManager<ApplicationUser> userManager, ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public CreateUserViewModel Input { get; set; } = new CreateUserViewModel();

        public List<string> AvailableRoles { get; set; } = new List<string>();

        public IActionResult OnGet()
        {
            AvailableRoles = GetAvailableRolesForCurrentUser();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                AvailableRoles = GetAvailableRolesForCurrentUser();
                return Page();
            }

            // Check if current user can create this role
            if (!CanCreateRole(Input.Role))
            {
                ModelState.AddModelError(string.Empty, "You don't have permission to create this role.");
                AvailableRoles = GetAvailableRolesForCurrentUser();
                return Page();
            }

            // Create user
            var currentUser = await _userManager.GetUserAsync(User);
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                PhoneNumber = Input.PhoneNumber,
                CreatedById = currentUser?.Id  // Track who created this user
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully", Input.Email);
                
                // Assign role
                var roleResult = await _userManager.AddToRoleAsync(user, Input.Role);
                
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign role {Role} to user {Email}", Input.Role, Input.Email);
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, $"Role assignment error: {error.Description}");
                    }
                    
                    // Delete the user since role assignment failed
                    await _userManager.DeleteAsync(user);
                    AvailableRoles = GetAvailableRolesForCurrentUser();
                    return Page();
                }
                
                _logger.LogInformation("Role {Role} assigned to user {Email}", Input.Role, Input.Email);
                
                TempData["Success"] = $"User {Input.FullName} created successfully with role {Input.Role}!";
                return RedirectToPage("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            AvailableRoles = GetAvailableRolesForCurrentUser();
            return Page();
        }

        private List<string> GetAvailableRolesForCurrentUser()
        {
            if (User.IsInRole(Roles.SuperAdmin))
            {
                return new List<string> { Roles.Admin, Roles.Staff, Roles.Client };
            }
            else if (User.IsInRole(Roles.Admin))
            {
                return new List<string> { Roles.Staff, Roles.Client };
            }
            else if (User.IsInRole(Roles.Staff))
            {
                return new List<string> { Roles.Client };
            }

            return new List<string>();
        }

        private bool CanCreateRole(string role)
        {
            if (User.IsInRole(Roles.SuperAdmin))
            {
                return true;
            }
            else if (User.IsInRole(Roles.Admin))
            {
                return role == Roles.Staff || role == Roles.Client;
            }
            else if (User.IsInRole(Roles.Staff))
            {
                return role == Roles.Client;
            }

            return false;
        }
    }
}
