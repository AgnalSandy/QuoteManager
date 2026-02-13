using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Models;
using QuoteManager.ViewModels;
using QuoteManager.Constants;

namespace QuoteManager.Pages.Users
{
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "No Role";

                // Hide SuperAdmin from the list (only one SuperAdmin should exist)
                if (role == "SuperAdmin")
                {
                    continue;
                }

                // If current user is Admin or Staff, only show users they created
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    if (user.CreatedById != currentUser?.Id)
                    {
                        continue; // Skip users not created by this Admin/Staff
                    }
                }

                // SuperAdmin sees all users (except other SuperAdmins)

                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber,
                    Role = role
                });
            }

            return Page();
        }


        [Authorize(Roles = "SuperAdmin,Admin,Staff")]
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Check 1: Prevent self-deletion
            if (currentUser?.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account!";
                return RedirectToPage();
            }

            // Check 2: Check if user has created other users (has children)
            var hasCreatedUsers = await _userManager.Users
                .AnyAsync(u => u.CreatedById == user.Id);

            if (hasCreatedUsers)
            {
                var createdUsersCount = await _userManager.Users
                    .CountAsync(u => u.CreatedById == user.Id);

                TempData["Error"] = $"Cannot delete {user.FullName} because they have created {createdUsersCount} user(s). Please reassign or delete those users first.";
                return RedirectToPage();
            }

            // Check 3: Permission check - ensure user can delete this role
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();

            if (!CanDeleteRole(userRole))
            {
                TempData["Error"] = "You don't have permission to delete users with this role.";
                return RedirectToPage();
            }

            // If all checks pass, attempt deletion
            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"User {user.FullName} deleted successfully!";
                }
                else
                {
                    TempData["Error"] = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (DbUpdateException ex)
            {
                // Catch any database-level errors (foreign key constraints, etc.)
                TempData["Error"] = "Cannot delete this user due to database constraints. They may have related records in the system.";
                // Log the exception for debugging
                Console.WriteLine($"Database error deleting user: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                TempData["Error"] = "An unexpected error occurred while deleting the user.";
                // Log the exception for debugging
                Console.WriteLine($"Unexpected error deleting user: {ex.Message}");
            }

            return RedirectToPage();
        }




        private bool CanDeleteRole(string role)
        {
            if (User.IsInRole(Roles.SuperAdmin))
            {
                // SuperAdmin can delete anyone except other SuperAdmins
                return role == Roles.Admin || role == Roles.Staff || role == Roles.Client;
            }
            else if (User.IsInRole(Roles.Admin))
            {
                // Admin can only delete Staff and Clients
                return role == Roles.Staff || role == Roles.Client;
            }
            else if (User.IsInRole(Roles.Staff))
            {
                // Staff can only delete Clients
                return role == Roles.Client;
            }

            return false;
        }









    }
}
